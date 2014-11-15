#r "System.Windows.Forms"
#r "PresentationUI"
#r "PresentationCore"
#r "PresentationFramework"
#r "WindowsBase"
#r "System.Xaml"

// load the image library from the app
#load "Image.fs"

// simple F# script file, could be used for testing
// or trying out new ideas

open System.Windows.Forms
open System.Drawing
open DemoApp
open System.Windows.Media.Imaging

// add custom FSI printer for images, so that the images are actually displayed
// rather than default plain text output
fsi.AddPrinter<BitmapImage>(fun x -> sprintf "%f x %f" x.Width x.Height)
fsi.AddPrinter<Image>(fun x -> 
    (new Form(Width = x.Width, Height = x.Height, BackgroundImage = x)).Show()
    sprintf "%d x %d" x.Width x.Height)

// now one can play around with the image API without needing
// to change the app code or create a new project
let x = Img.load @"<some image path>"

x |> Img.cropped 100 0 (x.Bitmap.Width / 2) (x.Bitmap.Height / 2)