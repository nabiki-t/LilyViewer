module LilyViewerMod

open System
open System.Windows
open LilyViewer

//////////////////////////////////
// エントリポイント
[<STAThread>]
[<EntryPoint>]
let main(_) = 
    let WinObj = new LilyMainWindow()
    WinObj.Initialize()
    ( new Application() ).Run( WinObj.TheWindow )
