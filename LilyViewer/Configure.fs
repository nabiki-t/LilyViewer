namespace LilyViewer

open Microsoft.Win32

type Configure() =

    // キー名のルート
    static let RootKey = @"Software\nabiki_t\LilyViewer"

    // ページ送りの方向（true:右から左、false:左から右
    static member GetPageDirection() =
        use key = Registry.CurrentUser.OpenSubKey( RootKey )
        if key = null then
            true
        else
            let rval = key.GetValue( "PageDirection", "FALSE" ) :?> string
            if rval.ToUpper() = "TRUE" then
                true
            else
                false

    static member SetPageDirection ( v : bool ) =
        use key = Registry.CurrentUser.CreateSubKey ( RootKey )
        if key <> null then
            if v then
                key.SetValue( "PageDirection", "TRUE" )
            else
                key.SetValue( "PageDirection", "FALSE" )


    // サムネイルのサイズ
    static member GetSumbnailSize() =
        use key = Registry.CurrentUser.OpenSubKey( RootKey )
        if key = null then
            1
        else
            key.GetValue( "SumbnailSize", 1 ) :?> int

    static member SetSumbnailSize ( v : int ) =
        use key = Registry.CurrentUser.CreateSubKey ( RootKey )
        if key <> null then
            key.SetValue( "SumbnailSize", v )

    // キャッシュ保持期限
    static member GetCashLifetime() =
        use key = Registry.CurrentUser.OpenSubKey( RootKey )
        if key = null then
            2
        else
            key.GetValue( "CashLifetime", 2 ) :?> int

    static member SetCashLifetime ( v : int ) =
        use key = Registry.CurrentUser.CreateSubKey ( RootKey )
        if key <> null then
            key.SetValue( "CashLifetime", v )

    // 見開き2ページか否か
    static member GetTwoPageFlg() =
        use key = Registry.CurrentUser.OpenSubKey( RootKey )
        if key = null then
            false
        else
            let rval = key.GetValue( "TwoPage", "FALSE" ) :?> string
            if rval.ToUpper() = "TRUE" then
                true
            else
                false

    static member SetTwoPageFlg ( v : bool ) =
        use key = Registry.CurrentUser.CreateSubKey ( RootKey )
        if key <> null then
            if v then
                key.SetValue( "TwoPage", "TRUE" )
            else
                key.SetValue( "TwoPage", "FALSE" )

    // フォルダのパス名
    static member GetFolderPath() =
        use key = Registry.CurrentUser.OpenSubKey( RootKey )
        if key = null then
            ""
        else
            key.GetValue( "FolderPath", "" ) :?> string

    static member SetFolderPath ( v : string ) =
        use key = Registry.CurrentUser.CreateSubKey ( RootKey )
        if key <> null then
            key.SetValue( "FolderPath", v )

    // キャッシュ格納パス
    static member GetCachePath() =
        use key = Registry.CurrentUser.OpenSubKey( RootKey )
        if key = null then
            ""
        else
            key.GetValue( "CachePath", "" ) :?> string

    static member SetCachePath ( v : string ) =
        use key = Registry.CurrentUser.CreateSubKey ( RootKey )
        if key <> null then
            key.SetValue( "CachePath", v )

    // ウインドウのサイズ
    static member GetWindowSize() =
        use key = Registry.CurrentUser.OpenSubKey( RootKey )
        if key = null then
            ( 700.0, 500.0 )
        else
            let w = 
                try
                    System.Double.TryParse( key.GetValue( "WindowWidth", 700.0 ) :?> string )
                    |> snd
                with
                | e -> 700.0
            let h =
                try
                    System.Double.TryParse( key.GetValue( "WindowHeight", 500.0 ) :?> string )
                    |> snd
                with
                | _ -> 500.0
            ( w, h )

    static member SetWindowSize ( w : float, h : float ) =
        use key = Registry.CurrentUser.CreateSubKey ( RootKey )
        if key <> null then
            key.SetValue( "WindowWidth", w )
            key.SetValue( "WindowHeight", h )
    