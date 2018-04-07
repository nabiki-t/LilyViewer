namespace LilyViewer

open System
open System.Windows
open System.Windows.Controls
open System.IO
open System.Windows.Media.Imaging
open ViewConstants

type FolderViewControls =

    // フォルダ一覧のタイトル
    val m_FolderTitles : Button[]

    // フォルダ一覧のサムネイル
    val m_FolderSumbnailImage : Image[][]

    // フォルダ一覧のファイル名
    val m_FolderSumbnailText : TextBlock[][]

    // フォルダ一覧のタイトルのサイズ
    val m_FolderTitleSize : Size[]

    // フォルダ一覧の画像サイズ
    val m_FolderSumbnailImageSize : Size[][]

    // フォルダ一覧のファイル名のサイズ
    val m_FolderSumbnailTextSize : Size[][]

    // フォルダ一覧のレイアウト
    val m_FolderListTitleWidth : float // タイトル表示欄の幅
    val m_FolderColumnWidth : float[]  // 列の幅
    val m_FolderRowHeight : float[]    // 行の高さ

    // フォルダ表示部分のサイズ
    val m_FolderListSize : Size

    // フォルダ内画像一覧のファイル名
    val m_FileSumbnailText : Button[]

    // フォルダ内画像一覧のイメージ
    val m_FileSumbnailImage : Image[]

    // フォルダ内画像一覧のファイル名のサイズ
    val m_FileSumbnailTextSize : Size[]

    // フォルダ内画像一覧の画像サイズ
    val m_FileSumbnailImageSize : Size[]

    // 表示列数毎の必要なサイズ（0は未使用、1は常に0、2～128列に値を設定）
    val m_FileListNeedsTotalSize : Size[]

    // 表示列数毎、列毎の幅（未計算の場合はnull）
    val m_FileListColumnWidth : float[][]

    // 表示列数毎、行毎の高さ
    val m_FileListRowHeight : float[][]

    // 配置対象となるキャンバス
    val m_Canvas : Canvas

    // コンストラクタ
    new( argCanvas : Canvas, argFolder : FolderEntry, argFolderSel : FolderEntry -> unit, argFileSel : FolderEntry -> int -> unit ) =

        // サムネイルの表示サイズ
        let sumbnailSize = float <| Configure.GetSumbnailSize()

        // 作業用のTextBlock
        let workTextBlock = new TextBlock()
        //let workButton = new Button()

        // サブフォルダの一覧を取得
        let subfolderNames = argFolder.GetSubfolderNames()
        let subfolderCount = subfolderNames.Length

        // フォルダ名のコントロール
        let subfolderName =
            [|
                for i = 0 to subfolderCount - 1 do
                    let b = new Button( Content = Path.GetFileName( subfolderNames.[i] ) )
                    b.Click.Add ( fun _ -> argFolderSel <| argFolder.GetSubfolder( i ) )
                    yield b
            |]

        // フォルダのサムネイルの画像
        let subfolderImages =
            [|
                argFolder.PreloadSubfolderImage()
                for i in [| 0 .. subfolderCount - 1 |] ->
                    [|
                        let subfolderImages = argFolder.GetSubfolderImage( i )
                        for j in [| 0 .. subfolderImages.Length - 1 |] ->
                            let wbitmap = subfolderImages.[j]
                            let w = wbitmap.Width
                            let h = wbitmap.Height
                            let s = float( w * h )
                            if s > 1.0 then
                                let rate = Math.Sqrt( ( sumbnailSize + 1.0 ) * 10000.0 / s )
                                new Image(
                                    Source = wbitmap,
                                    Width = w * rate,
                                    Height = h * rate
                                )
                            else
                                new Image()
                    |]
            |]

        // フォルダのサムネイルの名前
        let subfolderImageNames =
            [|
                for i in [| 0 .. subfolderCount - 1 |] ->
                    [|
                        let subfolderImageNames = argFolder.GetSubfolderImageName( i )
                        for j in [| 0 .. subfolderImageNames.Length - 1 |] ->
                            new TextBlock( Text = Path.GetFileName( subfolderImageNames.[j] ) ) 
                    |]

            |]

        // フォルダ名のサイズ
        let subfolderNameSize =
            [|
                for i in [| 0 .. subfolderCount - 1 |] ->
                    let workButton = new Button()
                    workButton.Content <- Path.GetFileName( subfolderNames.[i] )
                    workButton.Measure( new Size( 100000.0, 100000.0 ) )
                    new Size( Math.Max( workButton.DesiredSize.Width, 50.0 ), workButton.DesiredSize.Height + 10.0 )
            |]

        // フォルダのサムネイルのサイズ
        let subfolderImageSize =
            [|
                for i in [| 0 .. subfolderCount - 1 |] ->
                    [|
                        for j in [| 0 .. subfolderImages.[i].Length - 1 |] ->
                            new Size(
                                Width = subfolderImages.[i].[j].Width,
                                Height = subfolderImages.[i].[j].Height
                            )
                    |]
            |]

        // フォルダのサムネイルの名前のサイズ
        let subfolderImageNameSize =
            [|
                for i in [| 0 .. subfolderCount - 1 |] ->
                    [|
                        let subfolderImageNames = argFolder.GetSubfolderImageName( i )
                        for j in [| 0 .. subfolderImageNames.Length - 1 |] ->
                            workTextBlock.Text <- Path.GetFileName( subfolderImageNames.[j] )
                            workTextBlock.Measure( new Size( 100000.0, 100000.0 ) )
                            workTextBlock.DesiredSize
                    |]
            |]

        // フォルダ内の画像ファイル一覧を取得
        let imageNames = argFolder.GetImageNames()
        let imageCount = imageNames.Length

        // 名前
        let fileName =
            [|
                for i in [| 0 .. imageCount - 1 |] ->
                    let b = new Button( Content = Path.GetFileName( imageNames.[i] ) )
                    b.Click.Add ( fun _ -> argFileSel argFolder i )
                    b
            |]

        // 画像
        let fileImage =
            [|
                argFolder.PreloadMinimalImage()
                for i in [| 0 .. imageCount - 1 |] ->
                    let wbitmap = argFolder.GetMinimalImages( i )
                    let w = wbitmap.Width
                    let h = wbitmap.Height
                    let s = float( w * h )
                    if s > 1.0 then
                        let rate = Math.Sqrt( ( sumbnailSize + 1.0 ) * 10000.0 / s )
                        new Image(
                            Source = wbitmap,
                            Width = w * rate,
                            Height = h * rate
                        )
                    else
                        new Image()
            |]

        // 名前のサイズ
        let fileNameSize =
            [|
                for i = 0 to imageCount - 1 do
                    let workButton = new Button()
                    workButton.Content <- Path.GetFileName( imageNames.[i] )
                    workButton.Measure( new Size( 100000.0, 100000.0 ) )
                    yield new Size( Math.Max( workButton.DesiredSize.Width, 50.0 ), workButton.DesiredSize.Height + 10.0 )
            |]

        // 画像のサイズ
        let fileImageSize =
            [|
                for i in [| 0 .. imageCount - 1 |] ->
                    new Size(
                        Width = fileImage.[i].Width,
                        Height = fileImage.[i].Height
                    )
            |]

        let folderListTitleWidth : float = 
            let rec loop ( i : int ) ( m : float ) =
                if i < subfolderNameSize.Length then
                    loop ( i + 1 ) ( Math.Max( m, subfolderNameSize.[i].Width ) )
                else
                    m
            ( loop 0 0.0 ) + float( ViewConstants.COLUMN_SPACE )

        let folderColumnWidth =
            [|
                for i in [| 0 .. 2 |] ->
                    ( [|
                        if subfolderName.Length > 0 then
                            for j in [| 0 .. subfolderName.Length - 1 |] ->
                                Math.Max(
                                    if i < subfolderImageSize.[j].Length then
                                        subfolderImageSize.[j].[i].Width
                                    else
                                        0.0
                                    ,
                                    if i < subfolderImageNameSize.[j].Length then
                                        subfolderImageNameSize.[j].[i].Width
                                    else
                                        0.0
                                )
                        else
                            yield 0.0
                    |] |> Seq.max )
                    + float( ViewConstants.COLUMN_SPACE )
            |];
        let folderRowHeight =
            [|
                if subfolderName.Length > 0 then
                    for i in [| 0 .. subfolderName.Length - 1 |] do
                        let wv =
                            [|
                                for j in [| 0 .. subfolderImageNameSize.[i].Length - 1 |] ->
                                    subfolderImageSize.[i].[j].Height +
                                    subfolderImageNameSize.[i].[j].Height +
                                    float( ViewConstants.ROW_SPACE )
                            |]
                        if wv.Length > 0 then
                            yield ( Seq.max wv )
                        else
                            yield 0.0
                else
                    yield 0.0
            |];
        let folderListSize =
            new Size (
                folderListTitleWidth + Seq.sum folderColumnWidth,
                Seq.sum folderRowHeight + float( ViewConstants.SEPALATOR_HEIGHT )
            );

        {
            m_FolderTitles = subfolderName;
            m_FolderSumbnailImage = subfolderImages;
            m_FolderSumbnailText = subfolderImageNames;
            m_FolderTitleSize = subfolderNameSize;
            m_FolderSumbnailImageSize = subfolderImageSize;
            m_FolderSumbnailTextSize = subfolderImageNameSize;

            m_FolderListTitleWidth = folderListTitleWidth;
            m_FolderColumnWidth = folderColumnWidth;
            m_FolderRowHeight = folderRowHeight;
            m_FolderListSize = folderListSize;
            m_FileSumbnailImage = fileImage;
            m_FileSumbnailText = fileName;
            m_FileSumbnailTextSize = fileNameSize;
            m_FileSumbnailImageSize = fileImageSize;
            m_FileListNeedsTotalSize = Array.zeroCreate( 129 );
            m_FileListColumnWidth = Array.zeroCreate( 129 );
            m_FileListRowHeight = Array.zeroCreate( 129 );
            m_Canvas = argCanvas;
        }


    // サイズ変更
    interface IAddedControls with
        override this.OnSize ( wndSizeX : float ) ( wndSizeY : float ) ( updateOnly : bool ) =

            // フォルダ一覧の位置を設定する
            for i = 0 to this.m_FolderTitles.Length - 1 do
                let wsum1 = 
                    if i > 0 then
                        this.m_FolderRowHeight.[ 0 .. i - 1 ] |> Seq.sum
                    else
                        0.0
                // タイトルの位置を確定し、表示する
                this.m_FolderTitles.[i].Width <- this.m_FolderTitleSize.[i].Width
                this.m_FolderTitles.[i].Height <- this.m_FolderTitleSize.[i].Height

                Canvas.SetLeft( this.m_FolderTitles.[i], this.m_FolderListTitleWidth / 2.0 - this.m_FolderTitleSize.[i].Width / 2.0 )
                Canvas.SetTop( this.m_FolderTitles.[i], wsum1 + this.m_FolderRowHeight.[i] / 2.0 - this.m_FolderTitleSize.[i].Height / 2.0 )

                if not updateOnly then
                    this.m_Canvas.Children.Add( this.m_FolderTitles.[i] ) |> ignore

                // フォルダ一覧のサムネイルと名前を配置する
                for j = 0 to this.m_FolderSumbnailTextSize.[i].Length - 1 do
                    let img_txt_h = this.m_FolderSumbnailTextSize.[i].[j].Height + this.m_FolderSumbnailImageSize.[i].[j].Height
                    let wsum2 =
                        if j > 0 then
                            this.m_FolderColumnWidth.[ 0 .. j - 1 ] |> Seq.sum
                        else
                            0.0
                        + this.m_FolderListTitleWidth
                    Canvas.SetLeft( this.m_FolderSumbnailText.[i].[j], wsum2 + this.m_FolderColumnWidth.[j] / 2.0 - this.m_FolderSumbnailTextSize.[i].[j].Width / 2.0 )
                    Canvas.SetTop( this.m_FolderSumbnailText.[i].[j], wsum1 + this.m_FolderRowHeight.[i] / 2.0 - img_txt_h / 2.0 + this.m_FolderSumbnailImageSize.[i].[j].Height )
                    Canvas.SetLeft( this.m_FolderSumbnailImage.[i].[j], wsum2 + this.m_FolderColumnWidth.[j] / 2.0 - this.m_FolderSumbnailImageSize.[i].[j].Width / 2.0 )
                    Canvas.SetTop( this.m_FolderSumbnailImage.[i].[j], wsum1 + this.m_FolderRowHeight.[i] / 2.0 - img_txt_h / 2.0 )

                    if not updateOnly then
                        this.m_Canvas.Children.Add( this.m_FolderSumbnailText.[i].[j] ) |> ignore
                        this.m_Canvas.Children.Add( this.m_FolderSumbnailImage.[i].[j] ) |> ignore

            // 画像一覧の位置を設定する

            // 適切な列数を求める
            let targetWidth = wndSizeX - 20.0
            let rec loop ( s : int ) ( e : int ) =
                let m = ( s + e ) / 2
                if s + 1 >= e then
                    m
                else
                    // 必要な幅を取得する
                    if this.CalculateFileListNeedsSize( m ).Width < targetWidth then
                        loop m e
                    else
                        loop s m
            let fileListColumnCount =
                let wi = Math.Max( Math.Min( 128, loop 1 128 ), 1 )
                if this.CalculateFileListNeedsSize( wi ).Width >= targetWidth then
                    Math.Max( wi - 1, 1 )
                else
                    wi
            let fileListAreaSize = this.CalculateFileListNeedsSize( fileListColumnCount )

            // ファイル一覧の要素を配置する
            let rec loop ( i : int ) ( wsum1 : float ) =
                // 行番号
                let rowno = i / fileListColumnCount

                // 行の高さ
                let rowH = this.m_FileListRowHeight.[ fileListColumnCount ].[rowno]

                for j = i to Math.Min( this.m_FileSumbnailImage.Length, i + fileListColumnCount ) - 1 do
                    let wsum2 =
                        if j = i then
                            0.0
                        else
                            this.m_FileListColumnWidth.[ fileListColumnCount ].[ 0 .. j - 1 - i ] |> Array.sum

                    // 行の幅
                    let colW = this.m_FileListColumnWidth.[ fileListColumnCount ].[ j - i ]

                    // 画像とテキストを足した高さ
                    let img_txt_h = this.m_FileSumbnailTextSize.[j].Height + this.m_FileSumbnailImageSize.[j].Height

                    // ファイル名のサイズを設定する
                    this.m_FileSumbnailText.[j].Width <- this.m_FileSumbnailTextSize.[j].Width
                    this.m_FileSumbnailText.[j].Height <- this.m_FileSumbnailTextSize.[j].Height

                    // ファイル名の位置を設定する
                    Canvas.SetLeft( this.m_FileSumbnailText.[j], wsum2 + colW / 2.0 - this.m_FileSumbnailTextSize.[j].Width / 2.0 )
                    Canvas.SetTop( this.m_FileSumbnailText.[j], this.m_FolderListSize.Height + wsum1 + rowH / 2.0 - img_txt_h / 2.0 + this.m_FileSumbnailImageSize.[j].Height )

                    // 画像の表示位置を決定する
                    Canvas.SetLeft( this.m_FileSumbnailImage.[j], wsum2 + colW / 2.0 - this.m_FileSumbnailImageSize.[j].Width / 2.0 )
                    Canvas.SetTop( this.m_FileSumbnailImage.[j], this.m_FolderListSize.Height + wsum1 + rowH / 2.0 - img_txt_h / 2.0 )

                    if not updateOnly then
                        this.m_Canvas.Children.Add( this.m_FileSumbnailText.[j] ) |> ignore
                        this.m_Canvas.Children.Add( this.m_FileSumbnailImage.[j] ) |> ignore

                if i + fileListColumnCount < this.m_FileSumbnailImage.Length then
                    loop ( i + fileListColumnCount ) ( wsum1 + rowH )
            
            if this.m_FileSumbnailImage.Length > 0 then
                loop 0 ( float ViewConstants.SEPALATOR_HEIGHT )

            // キャンバスのサイズを設定する
            let w = Math.Max( this.m_FolderListSize.Width, fileListAreaSize.Width ) 
            let h = this.m_FolderListSize.Height + fileListAreaSize.Height + 35.0;
            this.m_Canvas.Width <- Math.Max( wndSizeX, w )
            this.m_Canvas.Height <- Math.Max( wndSizeY, h )

        // 表示している画面の種別を応答する
        override this.GetViewType() =
            0

    // ある列数にした場合における、必要な幅と各列の幅を算出する
    member this.CalculateFileListNeedsSize( ccnt : int ) =
        // すでに計算してあるのなら、その値を返して終わる
        if this.m_FileListColumnWidth.[ ccnt ] <> null then
            this.m_FileListNeedsTotalSize.[ ccnt ]
        else
            // 列幅を求める
            this.m_FileListColumnWidth.[ccnt] <-
                [|
                    for j in [| 0 .. ccnt - 1 |] ->
                        if this.m_FileSumbnailImage.Length > 0 then
                            let wv =
                                [|
                                    for i in [| j .. ccnt .. this.m_FileSumbnailImage.Length - 1 |] ->
                                        Math.Max(
                                            this.m_FileSumbnailImageSize.[i].Width,
                                            this.m_FileSumbnailTextSize.[i].Width
                                        )
                                |]
                            if wv.Length > 0 then
                                wv |> Array.max
                            else
                                0.0
                        else
                            0.0
                        + float( ViewConstants.COLUMN_SPACE )
                |]
                
            // 行の高さを求める
            this.m_FileListRowHeight.[ccnt] <-
                let rowCount = 
                    this.m_FileSumbnailImage.Length / ccnt +
                    if this.m_FileSumbnailImage.Length % ccnt > 0 then 1 else 0
                [|
                    if this.m_FileSumbnailImage.Length > 0 then
                        for i in [| 0 .. ccnt .. this.m_FileSumbnailImage.Length - 1 |] ->
                            [|
                                for j in [| 0 .. ccnt - 1 |] ->
                                    let vidx = i + j
                                    if vidx < this.m_FileSumbnailImage.Length then
                                        this.m_FileSumbnailImageSize.[ vidx ].Height +
                                        this.m_FileSumbnailTextSize.[ vidx ].Height +
                                        float( ViewConstants.ROW_SPACE )
                                    else
                                        0.0
                            |] |> Array.max
                     else
                        yield 0.0
                |]

            // 表示するために必要となる領域を求める
            this.m_FileListNeedsTotalSize.[ccnt].Width <-
                Array.sum this.m_FileListColumnWidth.[ ccnt ]
            this.m_FileListNeedsTotalSize.[ccnt].Height <-
                Array.sum this.m_FileListRowHeight.[ ccnt ]
                
            this.m_FileListNeedsTotalSize.[ccnt]




