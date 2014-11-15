namespace ImageCropper

open System.Windows.Media.Imaging
open System.Drawing
open System
open System.IO
open System.Drawing.Imaging

/// Helpers for processing bitmap images
module Img = 
    /// Wraps together System.Drawing.Bitmap (desktop use)
    /// and System.Windows.Media.Imaging.Bitmap (WPF use) [why isn't this easy??]
    type ImgWrapper = 
        { Bitmap : Bitmap
          BitmapImage : BitmapImage }
    
    /// Loads an image from a file path
    let load path = 
        { Bitmap = Bitmap.FromFile(path) :?> Bitmap
          BitmapImage = BitmapImage(Uri(path)) }
    
    /// Saves an image to a file
    let save (path : string) { Bitmap = bmp } = bmp.Save(path, bmp.RawFormat)
    
    /// Crops an image to a rectangle positioned at the 
    /// specified point, with the specified width and height
    let cropped x y width height bmp = 
        // nice, simple API for desktop bitmap
        let newBmp = bmp.Bitmap.Clone(Rectangle(x, y, width, height), bmp.Bitmap.PixelFormat)
        
        // much more ceremony required for WPF bitmap.
        // this is why we can't have nice things
        let newBmpImg = 
            use stream = new MemoryStream()
            newBmp.Save(stream, ImageFormat.Png)
            stream.Position <- 0L
            let result = BitmapImage()
            result.BeginInit()
            result.StreamSource <- stream
            result.CacheOption <- BitmapCacheOption.OnLoad
            result.EndInit()
            result
        { Bitmap = newBmp
          BitmapImage = newBmpImg }