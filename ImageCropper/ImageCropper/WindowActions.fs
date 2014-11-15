namespace ImageCropper

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open FSharpx
open Microsoft.Win32
open Img

/// Wrapper to simplify file dialog actions
module ImageFileDialog = 
    let private imageFilter = "Image files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png"
    let private saveDiag = SaveFileDialog(Filter = imageFilter)
    let private openDiag = 
        OpenFileDialog
            (Filter = imageFilter, Multiselect = false, 
             InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
    
    /// Quickie active pattern for nullables
    let (|Null|Value|) (x : Nullable<_>) = 
        if x.HasValue then Value(x.Value)
        else Null
    
    /// Runs the open file dialog, returning an
    /// option containing the selected path
    let openSingleFile() = 
        match openDiag.ShowDialog() with
        | Value(true) -> Some(openDiag.FileName)
        | _ -> None
    
    /// Runs the save file dialog, returning an
    /// option containing the selected save path
    let saveFile() = 
        match saveDiag.ShowDialog() with
        | Value(true) -> Some(saveDiag.FileName)
        | _ -> None

/// Core logic for UI actions
module WindowActions =

    // XAML type provider used for primary window type.
    // Point the type provider at the XAML file and it injects types/methods/etc
    // dynamically at compile-time.
    type MainWindow = XAML< "MainWindow.xaml" >
    
    // a little bit of mutable state
    let private resizingRect = ref false
    let private movingRect = ref false
    let private moveOffset = ref (0., 0.)
    let private image : ImgWrapper option ref = ref None

    /// Gets (left coord, top coord, width, height) of the selection rectangle
    /// relative to the main image control
    let getRect (window : MainWindow) = 
        (Canvas.GetLeft(window.mainRect) - Canvas.GetLeft(window.mainImage), 
         Canvas.GetTop(window.mainRect) - Canvas.GetTop(window.mainImage),
         window.mainRect.Width,
         window.mainRect.Height)

    /// Sets (left coord, top coord, width, height) of the selection rectangle
    /// relative to the main image control
    let setRect (x, y, w, h) (window : MainWindow) = 
        Canvas.SetLeft(window.mainRect, x + Canvas.GetLeft(window.mainImage))
        Canvas.SetTop(window.mainRect, y + Canvas.GetTop(window.mainImage))

        // ensure the rectangle has some min size and doesn't disappear
        window.mainRect.Width <- max 5. w
        window.mainRect.Height <- max 5. h
    
    /// Handles "load file" user action
    let loadFileClicked (window : MainWindow) _ = 
        match ImageFileDialog.openSingleFile() with
        | None -> ()
        | Some(filePath) -> 
            // load the image and display it
            image := filePath |> Img.load |> Some
            window.mainImage.Source <- Option.get(!image).BitmapImage

            // put the crop rectangle in the corner with some default size
            window |> setRect (0., 0., 100., 100.)

            // get the rest of the UI ready
            window.fileNameTextBox.Text <- filePath
            window.mainRect.Visibility <- Visibility.Visible
            window.saveButton.IsEnabled <- true
    
    /// Handles "save file" user action
    let saveFileClicked (window : MainWindow) _ = 
        match ImageFileDialog.saveFile() with
        | None -> ()
        | Some(filePath) -> 
            let (x, y, w, h) = window |> getRect

            // There might or might not be an image loaded - using
            // an option type forces us to consider both cases.
            match !image with
            | None -> ()
            | Some(img) -> 
                let scale = (float img.Bitmap.Width) / window.mainImage.ActualWidth
                let (scaleX, scaleY, scaleW, scaleH) = (x * scale, y * scale, w * scale, h * scale)
                img
                |> Img.cropped (int scaleX) (int scaleY) (int scaleW) (int scaleH)
                |> Img.save filePath
                window.saveFileTextBox.Text <- sprintf "Saved to %s" filePath
    
    /// Active pattern for detecting how a given point
    /// relates to the crop rectangle
    let private (|Outside|Inside|BottomRight|) (window : MainWindow, pt : System.Windows.Point) = 
        let x, y, w, h = window |> getRect
        let inRegion   = pt.X >= x && pt.X <= x + w && pt.Y >= y && pt.Y <= y + h
        let nearBottom = (abs (pt.Y - (y + h))) <= 30.
        let nearRight  = (abs (pt.X - (x + w))) <= 30.

        // the 3 cases we are interested in are now easy to detect
        match inRegion,nearBottom,nearRight with
        | false, _, _ -> Outside
        | true, true, true -> BottomRight
        | _ -> Inside(pt.X - x, pt.Y - y)  // return the offsets along with this case
    
    /// Handles mouse button down
    let mouseDown (window : MainWindow) (e : MouseButtonEventArgs) =
        // logic here is extremely clean and readable thanks to our active pattern
        match (window, e.GetPosition(window.mainImage)) with
        | BottomRight ->
            resizingRect := true
        | Inside(offsets) ->
            movingRect := true
            moveOffset := offsets
        | Outside -> ()
    
    /// Handles mouse button up
    let mouseUp _ _ = 
        resizingRect := false
        movingRect := false
    
    /// Handles resizing the crop rectangle with mouse movement
    let mouseMove (window : MainWindow) (e : MouseEventArgs) = 
        let pt = e.GetPosition(window.mainImage)
        let (x, y, w, h) = window |> getRect
        match e.LeftButton, !resizingRect, !movingRect with
        | (MouseButtonState.Pressed, true, false) ->
            window |> setRect (x, y, pt.X - x, pt.Y - y)
        | (MouseButtonState.Pressed, false, true) ->
            let xOffset,yOffset = !moveOffset
            window |> setRect (pt.X - xOffset, pt.Y - yOffset, w, h)
        | _ -> ()  