module MainApp

open System
open System.Windows
open ImageCropper
open ImageCropper.WindowActions

let loadWindow () =
    // the primary window object used throughout the code
    // comes from the XAML type provider
    let window = MainWindow()
    window.Root.ResizeMode <- ResizeMode.NoResize   

    // hook up event handlers
    // heavy (ab)use of partial-application here
    window |> WindowActions.loadFileClicked |> window.loadButton.Click.Add
    window |> WindowActions.saveFileClicked |> window.saveButton.Click.Add

    window |> WindowActions.mouseDown |> window.mainRect.MouseDown.Add
    window |> WindowActions.mouseDown |> window.mainImage.MouseDown.Add

    window |> WindowActions.mouseUp |> window.mainRect.MouseUp.Add
    window |> WindowActions.mouseUp |> window.mainImage.MouseUp.Add
    
    window |> WindowActions.mouseMove |> window.mainRect.MouseMove.Add
    window |> WindowActions.mouseMove |> window.mainImage.MouseMove.Add
    
    window.Root

[<STAThread>]
(new Application()).Run(loadWindow ()) |> ignore