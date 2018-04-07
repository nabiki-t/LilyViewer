namespace LilyViewer

open System
open System.IO
open System.Windows
open System.Windows.Controls
open System.Windows.Media.Imaging
open System.Windows.Input 
open System.Diagnostics

// メインウインドウ
type LilyMainWindow() =
    
    // XAMLのコードをリソースからロードする
    let m_Window =
        Application.LoadComponent(
            new System.Uri( "/LilyViewer;component/mainwindow.xaml", UriKind.Relative )
        ) :?> Window

    // コントールのオブジェクトを取得する
    let m_TitleText = m_Window.FindName( "TitleText" ) :?> TextBlock
    let m_TwoPageButton = m_Window.FindName( "TwoPageButton" ) :?> Button
    let m_LeftButton = m_Window.FindName( "LeftButton" ) :?> Button
    let m_RightButton = m_Window.FindName( "RightButton" ) :?> Button
    let m_UpToButton = m_Window.FindName( "UpToButton" ) :?> Button
    let m_OpenExplorerButton = m_Window.FindName( "OpenExplorerButton" ) :?> Button
    let m_ConfigureButton = m_Window.FindName( "ConfigureButton" ) :?> Button
    let m_MainCanvas = m_Window.FindName( "MainCanvas" ) :?> Canvas

    let m_TwoPageButtonImage = m_Window.FindName( "TwoPageButtonImage" ) :?> Image
    let m_LeftButtonImage = m_Window.FindName( "LeftButtonImage" ) :?> Image
    let m_RightButtonImage = m_Window.FindName( "RightButtonImage" ) :?> Image
    let m_UpToButtonImage = m_Window.FindName( "UpToButtonImage" ) :?> Image
    let m_ConfigureButtonImage = m_Window.FindName( "ConfigureButtonImage" ) :?> Image
    let m_MainScroll = m_Window.FindName( "MainScroll" ) :?> ScrollViewer

    // コントロール用のビットマップを構築しておく
    let m_twopageBmp = new BitmapImage( new Uri( "/LilyViewer;component/Assets/twopage.png", UriKind.Relative ) )
    let m_singlepageBmp = new BitmapImage( new Uri( "/LilyViewer;component/Assets/singlepage.png", UriKind.Relative ) )
    let m_up_dBmp = new BitmapImage( new Uri( "/LilyViewer;component/Assets/up_d.png", UriKind.Relative ) )
    let m_left_dBmp = new BitmapImage( new Uri( "/LilyViewer;component/Assets/left_d.png", UriKind.Relative ) )
    let m_right_dBmp = new BitmapImage( new Uri( "/LilyViewer;component/Assets/right_d.png", UriKind.Relative ) )
    let m_upBmp = new BitmapImage( new Uri( "/LilyViewer;component/Assets/up.png", UriKind.Relative ) )
    let m_leftBmp = new BitmapImage( new Uri( "/LilyViewer;component/Assets/left.png", UriKind.Relative ) )
    let m_rightBmp = new BitmapImage( new Uri( "/LilyViewer;component/Assets/right.png", UriKind.Relative ) )

    // コントロール
    let mutable m_Controls : IAddedControls =
        ( new BlankPageControls( m_Window.FindName( "MainCanvas" ) :?> Canvas ) ) :> IAddedControls

    // フォルダの情報
    let mutable m_FolderEntry : FolderEntry =
        new FolderEntry( "", None, 0 )

    // 古いキャッシュの削除処理を停止するためのトークン
    let m_OcdCancelToken = new Threading.CancellationTokenSource()


    member this.TheWindow with get() = m_Window

    // ウインドウ全体の初期化
    member this.Initialize () =
        // ボタンのイベントハンドラを追加
        m_TwoPageButton.Click.AddHandler ( fun sender e -> this.TwoPageButton_Click sender e )
        m_LeftButton.Click.AddHandler ( fun sender e -> this.LeftButton_Click sender e )
        m_RightButton.Click.AddHandler ( fun sender e -> this.RightButton_Click sender e )
        m_UpToButton.Click.AddHandler ( fun sender e -> this.UpToButton_Click sender e )
        m_OpenExplorerButton.Click.AddHandler ( fun sender e -> this.OpenExplorerButton_Click sender e )
        m_ConfigureButton.Click.AddHandler ( fun sender e -> this.ConfigureButton_Click sender e )

        // サイズ変更のイベントを追加
        m_Window.SizeChanged.AddHandler ( fun sender e -> this.OnSize sender e )

        // キー押下のイベントを追加
        m_Window.KeyDown.AddHandler ( fun sender e -> this.OnKeyDown sender e )
        m_MainScroll.KeyDown.AddHandler ( fun sender e -> this.OnKeyDown sender e )
        m_MainScroll.Focusable <- false

        m_Window.Closed.AddHandler ( fun sender e -> this.OnClosed sender e )

        // 初期状態のフォルダを表示する
        let initFolderName = Configure.GetFolderPath()
        let nextFE = new FolderEntry( initFolderName, None, 0 )
        this.UpdateView nextFE ( new FolderViewControls( m_MainCanvas, nextFE, this.OnSelectFolder, this.OnSelectImage ) )

        // 初期状態ではフォルダ表示（もしくはブランク）のはずだから、スクロールバーを表示しておく
        m_MainScroll.VerticalScrollBarVisibility <- ScrollBarVisibility.Visible
        m_MainScroll.HorizontalScrollBarVisibility <- ScrollBarVisibility.Visible

        // ウインドウの初期サイズを設定する
        let ( initWidth, initHeight ) = Configure.GetWindowSize()
        m_Window.Width <- initWidth
        m_Window.Height <- initHeight

        // 古いキャッシュを削除する処理を実行する
        let ocdProc =
            async {
                let nt = DateTime.Now   // 現在時刻
                let clt =               // キャッシュの保持期間
                    match Configure.GetCashLifetime() with
                    | 0 ->  // 1週間
                        7 * 24 * 3600
                    | 1 ->  // 1ヶ月
                        30 * 24 * 3600
                    | _ ->  // 1年間
                        365 * 24 * 3600

                // キャッシュを格納しているパス名を取得する
                let path =
                    let wp = Configure.GetCachePath()
                    if wp.Length = 0 then
                        Path.GetTempPath() + "\\LilyViewerCache\\"
                    else
                        wp + "\\"
                
                // キャッシュを走査して古いものを削除する
                for d1 in Directory.EnumerateDirectories( path ) do
                    for d2 in Directory.EnumerateDirectories d1 do
                        for f1 in Directory.EnumerateFiles d2 do
                            let lat = File.GetLastAccessTime( f1 )
                            if Math.Abs( nt.Subtract( lat ).TotalSeconds ) > float clt then
                                // 一定期間経過しているため削除する
                                try
                                    File.Delete f1
                                with
                                | _ -> ()
            }
        try
            Async.Start( ocdProc, m_OcdCancelToken.Token )
        with
        | _ -> ()

    // 見開き2ページボタンの押下
    member this.TwoPageButton_Click sender e =
        let viewType = m_Controls.GetViewType()
        if viewType = 1 then
            let twoPageFlg = Configure.GetTwoPageFlg()
            let ivc = m_Controls :?> ImageViewControls
            let i = ivc.GetPageIndex()
            Configure.SetTwoPageFlg ( not twoPageFlg )
            this.UpdateView m_FolderEntry ( new ImageViewControls( m_MainCanvas, m_FolderEntry, i ) )

    // 左ボタンの押下
    member this.LeftButton_Click sender e =
        let viewType = m_Controls.GetViewType()
        if viewType = 1 then
            // 画像表示の場合
            if Configure.GetPageDirection() then
                // 右から左に進むのであれば、次のページを表示する
                this.ShowNextPage 1
            else
                // 左から右に進むのであれば、前のページを表示する
                this.ShowBeforePage 1
        elif viewType = 0 then
            // フォルダ表示の場合
            let parent = m_FolderEntry.GetParent()
            let siglingsIndex = m_FolderEntry.GetSiblingsIndex()
            if Configure.GetPageDirection() then
                // 親が存在し、かつ、次の兄弟が存在するのであれば、その兄弟のフォルダを表示する
                if parent.IsSome then
                    let siblingsCount = parent.Value.GetSubfolderNames().Length
                    if siglingsIndex + 1 < siblingsCount then
                        let next = parent.Value.GetSubfolder( siglingsIndex + 1 )
                        this.UpdateView next ( new FolderViewControls( m_MainCanvas, next, this.OnSelectFolder, this.OnSelectImage ) )
            else
                // 親が存在し、かつ、1つ前の兄弟が存在するのであれば、その兄弟のフォルダを表示する
                if parent.IsSome && siglingsIndex > 0 then
                    let next = parent.Value.GetSubfolder( siglingsIndex - 1 )
                    this.UpdateView next ( new FolderViewControls( m_MainCanvas, next, this.OnSelectFolder, this.OnSelectImage ) )

    // 右ボタンの押下
    member this.RightButton_Click sender e =
        let viewType = m_Controls.GetViewType()
        if viewType = 1 then
            // 画像表示の場合
            if Configure.GetPageDirection() then
                // 右から左に進むのであれば、前のページを表示する
                this.ShowBeforePage 1
            else
                // 左から右に進むのであれば、次のページを表示する
                this.ShowNextPage 1
        elif viewType = 0 then
            // フォルダ表示の場合
            let parent = m_FolderEntry.GetParent()
            let siglingsIndex = m_FolderEntry.GetSiblingsIndex()
            if not ( Configure.GetPageDirection() ) then
                // 親が存在し、かつ、次の兄弟が存在するのであれば、その兄弟のフォルダを表示する
                if parent.IsSome then
                    let siblingsCount = parent.Value.GetSubfolderNames().Length
                    if siglingsIndex + 1 < siblingsCount then
                        let next = parent.Value.GetSubfolder( siglingsIndex + 1 )
                        this.UpdateView next ( new FolderViewControls( m_MainCanvas, next, this.OnSelectFolder, this.OnSelectImage ) )
            else
                // 親が存在し、かつ、1つ前の兄弟が存在するのであれば、その兄弟のフォルダを表示する
                if parent.IsSome && siglingsIndex > 0 then
                    let next = parent.Value.GetSubfolder( siglingsIndex - 1 )
                    this.UpdateView next ( new FolderViewControls( m_MainCanvas, next, this.OnSelectFolder, this.OnSelectImage ) )


    // 上ボタンの押下
    member this.UpToButton_Click sender e =
        let viewType = m_Controls.GetViewType()
        if viewType = 0 then
            if m_FolderEntry.GetParent().IsSome then
                // 親のフォルダへ遷移する
                let nextFE = m_FolderEntry.GetParent().Value
                this.UpdateView nextFE ( new FolderViewControls( m_MainCanvas, nextFE, this.OnSelectFolder, this.OnSelectImage ) )
        elif viewType = 1 then
            // 画像表示からフォルダ表示に戻る
            this.UpdateView m_FolderEntry ( new FolderViewControls( m_MainCanvas, m_FolderEntry, this.OnSelectFolder, this.OnSelectImage ) )

    // エクスプローラーで開くボタンの押下
    member this.OpenExplorerButton_Click sender e =
        let viewType = m_Controls.GetViewType()
        if viewType = 0 then
            // フォルダ表示の場合は、単に今表示しているフォルダを表示するのみとする
            Process.Start( "EXPLORER.EXE", m_FolderEntry.GetName() ) |> ignore
        elif viewType = 1 then
            // 画像を表示しているときは、今表示している画像を選択した状態で、エクスプローラを起動する
            let idx = ( m_Controls :?> ImageViewControls ).GetPageIndex()
            let fileName = m_FolderEntry.GetImageNames().[ idx ]
            Process.Start( "EXPLORER.EXE", "/select,\"" + fileName + "\"" ) |> ignore

        ()

    // 設定ボタンの押下
    member this.ConfigureButton_Click sender e =
        let d = new ConfigWindow()
        let beforeFolderPath = Configure.GetFolderPath().ToUpper()
        d.Initialize m_Window
        if d.Show() then
            // 表示を更新する
            if beforeFolderPath <> Configure.GetFolderPath().ToUpper() then
                // フォルダを開きなおす
                let nextFE = new FolderEntry( Configure.GetFolderPath(), None, 0 )
                this.UpdateView nextFE ( new FolderViewControls( m_MainCanvas, nextFE, this.OnSelectFolder, this.OnSelectImage ) )
            else
                // フォルダはそのままで、表示しなおすのみ
                match m_Controls.GetViewType() with
                | 0 ->
                    this.UpdateView m_FolderEntry ( new FolderViewControls( m_MainCanvas, m_FolderEntry, this.OnSelectFolder, this.OnSelectImage ) )
                | 1 ->
                    let ivc = m_Controls :?> ImageViewControls
                    let i = ivc.GetPageIndex()
                    this.UpdateView m_FolderEntry ( new ImageViewControls( m_MainCanvas, m_FolderEntry, i ) )
                | _ ->
                    this.UpdateView m_FolderEntry ( new BlankPageControls( m_MainCanvas ) )


    // ウインドウのサイズ変更
    member this.OnSize sender e =
        m_Controls.OnSize ( m_MainScroll.ActualWidth ) ( m_MainScroll.ActualHeight ) true

    // キーが押下された
    member this.OnKeyDown sender e =
        let viewType = m_Controls.GetViewType()
        let twoPageFlg = Configure.GetTwoPageFlg()
        match e.Key with
        | Input.Key.Escape ->
            this.UpToButton_Click () null
        | Input.Key.Left ->
            this.LeftButton_Click () null
        | Input.Key.Right ->
            this.RightButton_Click () null
        | Input.Key.PageUp ->
            if viewType = 1 then
                this.ShowBeforePage <| if twoPageFlg then 2 else 1
            elif viewType = 0 then
                m_MainScroll.PageUp()
        | Input.Key.PageDown ->
            if viewType = 1 then
                this.ShowNextPage <| if twoPageFlg then 2 else 1
            elif viewType = 0 then
                m_MainScroll.PageDown()
        | Input.Key.Home ->
            if viewType = 0 then
                m_MainScroll.ScrollToTop()
        | Input.Key.End ->
            if viewType = 0 then
                m_MainScroll.ScrollToBottom()
        | Input.Key.NumPad2
        | Input.Key.D2 ->
            if viewType = 1 then
                this.TwoPageButton_Click () null
        | _ ->
            ()
    
    // ウインドウが閉じられる
    member this.OnClosed sender e =
        Configure.SetWindowSize( m_Window.Width, m_Window.Height )

        // キャッシュ削除処理の停止を指示する
        m_OcdCancelToken.Cancel()

        // キャッシュ作成処理の停止を指示する
        m_FolderEntry.GetRoot().StopCreateAllCacheProc()

        exit 0

    // フォルダ名が選択された
    member this.OnSelectFolder ( f : FolderEntry ) =
        this.UpdateView f ( new FolderViewControls( m_MainCanvas, f, this.OnSelectFolder, this.OnSelectImage ) )

    // 画像名が選択された
    member this.OnSelectImage ( f : FolderEntry ) ( idx : int ) =
        this.UpdateView f ( new ImageViewControls( m_MainCanvas, f, idx ) )

    // 表示を更新する
    member this.UpdateView( nextFE : FolderEntry ) ( ctrls : IAddedControls ) =

        // 現在表示されている種別を取得
        let oldViewType = m_Controls.GetViewType()
        if oldViewType = 0 then
            // 現在、フォルダ表示が表示されていた場合には、スクロールバーの位置を保存しておく
            m_FolderEntry.SetScrollberPos m_MainScroll.VerticalOffset m_MainScroll.HorizontalOffset

        // 新しいルートを表示する場合には、実行中（かもしれない）キャッシュ再構築処理を停止する
        let isNewRootFolder = not ( m_FolderEntry.GetRoot() = nextFE.GetRoot() )
        if isNewRootFolder then
            m_FolderEntry.GetRoot().StopCreateAllCacheProc()

        m_FolderEntry <- nextFE
        m_Controls <- ctrls
        m_MainCanvas.Children.Clear()
        m_Controls.OnSize ( m_MainScroll.ActualWidth ) ( m_MainScroll.ActualHeight ) false

        let viewType = m_Controls.GetViewType()
        if viewType = 0 then
            // フォルダ表示の場合、左右と見開き2ページボタンは意味がない
            m_TwoPageButtonImage.Source <-
                if Configure.GetTwoPageFlg() then
                    m_twopageBmp
                else
                    m_singlepageBmp
            m_TwoPageButton.IsEnabled <- false

            let parent = m_FolderEntry.GetParent()
            if parent.IsNone then
                // 親フォルダが存在しないのならば、上・右・左のボタンは使えない
                m_UpToButtonImage.Source <- m_up_dBmp
                m_UpToButton.IsEnabled <- false
                m_LeftButtonImage.Source <- m_left_dBmp
                m_LeftButton.IsEnabled <- false
                m_RightButtonImage.Source <- m_right_dBmp
                m_RightButton.IsEnabled <- false
            else
                // 親フォルダが存在する場合は、上ボタンは常に使用可能である
                m_UpToButtonImage.Source <- m_upBmp
                m_UpToButton.IsEnabled <- true

                // 兄弟の数を取得する
                let siblingsCount = parent.Value.GetSubfolderNames().Length

                if Configure.GetPageDirection() then
                    // 右から左に進める場合

                    if m_FolderEntry.GetSiblingsIndex() <= 0 then
                        // 現在のインデックスが0であれば、これ以上右には行けない
                        m_RightButtonImage.Source <- m_right_dBmp
                        m_RightButton.IsEnabled <- false
                    else
                        // 右に進むことが可能
                        m_RightButtonImage.Source <- m_rightBmp
                        m_RightButton.IsEnabled <- true
                    
                    if m_FolderEntry.GetSiblingsIndex() >= siblingsCount - 1 then
                        // 現在のインデックスが、兄弟の末尾であれば、これ以上左には行けない
                        m_LeftButtonImage.Source <- m_left_dBmp
                        m_LeftButton.IsEnabled <- false
                    else
                        m_LeftButtonImage.Source <- m_leftBmp
                        m_LeftButton.IsEnabled <- true
                else
                    // 左から右に進める場合
                    if m_FolderEntry.GetSiblingsIndex() <= 0 then
                        // 現在のインデックスが0であれば、これ以上左には行けない
                        m_LeftButtonImage.Source <- m_left_dBmp
                        m_LeftButton.IsEnabled <- false
                    else
                        // 左に進むことが可能
                        m_LeftButtonImage.Source <- m_leftBmp
                        m_LeftButton.IsEnabled <- true
                    
                    if m_FolderEntry.GetSiblingsIndex() >= siblingsCount - 1 then
                        // 現在のインデックスが、兄弟の末尾であれば、これ以上右には行けない
                        m_RightButtonImage.Source <- m_right_dBmp
                        m_RightButton.IsEnabled <- false
                    else
                        // 右に進むことが可能
                        m_RightButtonImage.Source <- m_rightBmp
                        m_RightButton.IsEnabled <- true

            // スクロールバーを表示する
            m_MainScroll.VerticalScrollBarVisibility <- ScrollBarVisibility.Visible
            m_MainScroll.HorizontalScrollBarVisibility <- ScrollBarVisibility.Visible

            // フォルダのパス名を表示する
            m_TitleText.Text <- m_FolderEntry.GetName()

            // スクロールバーの位置を復元する
            let vscroll, hscroll = m_FolderEntry.GetScrollberPos()
            m_MainScroll.ScrollToVerticalOffset vscroll
            m_MainScroll.ScrollToHorizontalOffset hscroll


        elif viewType = 1 then
            // 画像表示の場合
            let twoPageFlg = Configure.GetTwoPageFlg()
            let ivc = m_Controls :?> ImageViewControls
            let i = ivc.GetPageIndex()
            let images = m_FolderEntry.GetImageNames()
            let imageCount = images.Length
            m_TwoPageButtonImage.Source <-
                if twoPageFlg then
                    m_twopageBmp
                else
                    m_singlepageBmp
            if Configure.GetPageDirection() then
                // 右から左に進める場合
                if ( twoPageFlg && i + 1 < imageCount - 1 ) || ( not twoPageFlg && i + 1 < imageCount ) then
                    m_LeftButtonImage.Source <- m_leftBmp
                    m_LeftButton.IsEnabled <- true
                else
                    m_LeftButtonImage.Source <- m_left_dBmp
                    m_LeftButton.IsEnabled <- false
                if i > 0 then
                    m_RightButtonImage.Source <- m_rightBmp
                    m_RightButton.IsEnabled <- true
                else
                    m_RightButtonImage.Source <- m_right_dBmp
                    m_RightButton.IsEnabled <- false
            else
                // 左から右に進める場合
                if ( twoPageFlg && i + 1 < imageCount - 1 ) || ( not twoPageFlg && i + 1 < imageCount ) then
                    m_RightButtonImage.Source <- m_rightBmp
                    m_RightButton.IsEnabled <- true
                else
                    m_RightButtonImage.Source <- m_right_dBmp
                    m_RightButton.IsEnabled <- false
                if i > 0 then
                    m_LeftButtonImage.Source <- m_leftBmp
                    m_LeftButton.IsEnabled <- true
                else
                    m_LeftButtonImage.Source <- m_left_dBmp
                    m_LeftButton.IsEnabled <- false

            m_UpToButtonImage.Source <- m_upBmp
            m_UpToButton.IsEnabled <- true
            m_TwoPageButton.IsEnabled <- true

            // 画像表示の場合は、スクロールバーは不要
            m_MainScroll.VerticalScrollBarVisibility <- ScrollBarVisibility.Hidden
            m_MainScroll.HorizontalScrollBarVisibility <- ScrollBarVisibility.Hidden

            // 画像のファイル名を表示する
            m_TitleText.Text <- Path.GetFileName( images.[i] )


        else
            // すべて無効化する
            m_TwoPageButtonImage.Source <- m_singlepageBmp
            m_LeftButtonImage.Source <- m_left_dBmp
            m_RightButtonImage.Source <- m_right_dBmp
            m_UpToButtonImage.Source <- m_up_dBmp
            m_TwoPageButton.IsEnabled <- false
            m_LeftButton.IsEnabled <- false
            m_RightButton.IsEnabled <- false
            m_UpToButton.IsEnabled <- false
            m_MainScroll.VerticalScrollBarVisibility <- ScrollBarVisibility.Visible
            m_MainScroll.HorizontalScrollBarVisibility <- ScrollBarVisibility.Visible

        // 新しくルートフォルダを表示する際には、キャッシュ再構築処理を開始する
        if isNewRootFolder then
            m_FolderEntry.CreateAllCache()

    // 1ページ進める
    member this.ShowNextPage ( step : int ) =
        let ivc = m_Controls :?> ImageViewControls
        let i = ivc.GetPageIndex() + step   // 次に表示したいページ
        let imageCount = m_FolderEntry.GetImageNames().Length
        if Configure.GetTwoPageFlg() then
            if i < imageCount - 1 then
                this.UpdateView m_FolderEntry ( new ImageViewControls( m_MainCanvas, m_FolderEntry, i ) )
        else
            if i < imageCount then
                this.UpdateView m_FolderEntry ( new ImageViewControls( m_MainCanvas, m_FolderEntry, i ) )

    // 1ページ戻る
    member this.ShowBeforePage ( step : int ) =
        let ivc = m_Controls :?> ImageViewControls
        let i = ivc.GetPageIndex() - step
        if i >= 0 then
            this.UpdateView m_FolderEntry ( new ImageViewControls( m_MainCanvas, m_FolderEntry, i ) )
