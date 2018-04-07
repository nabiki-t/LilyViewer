namespace LilyViewer

open System
open System.Text
open System.IO
open System.Security
open System.Windows.Media.Imaging
open System.Windows.Media

// 一覧に表示する、フォルダ1つ分の情報を保持する
type FolderEntry( argFolder : string, argParent : FolderEntry option, argSiblingsIndex : int ) as this =

    // 対象のディレクトリ
    let m_TargetFolder = argFolder

    // このフォルダの直下にある画像のファイル名
    let m_ImageNames : string[] =
        if argFolder <> "" then
            Directory.EnumerateFiles( argFolder )
            |> Seq.filter (
                fun itr ->
                    let w = Path.GetExtension( itr ).ToUpper()
                    ( w = ".JPG" || w = ".GIF" || w = ".PNG" || w = ".BMP" )
            )
            |> Seq.sort
            |> Seq.toArray
        else
            [||]

    // サムネイルの画像
    let m_MinimalImages : BitmapImage[] =
        Array.zeroCreate( m_ImageNames.Length );

    // サブフォルダ名
    let m_SubfolderNames : string[] =
        if argFolder <> "" then
            System.IO.Directory.EnumerateDirectories( argFolder )
            |> Seq.sort
            |> Seq.toArray
        else
            [||]

    // サブフォルダ
    let m_Subfolders : FolderEntry option [] =
        Array.zeroCreate( m_SubfolderNames.Length );

    // サブフォルダのサムネイル
    let m_SubfolderImage : BitmapImage[][] =
        Array.zeroCreate( m_SubfolderNames.Length );

    // サブフォルダのサムネイルのファイル名
    let m_SubfolderImageName : string[][] =
        Array.zeroCreate( m_SubfolderNames.Length );

    // 親フォルダ
    let m_Parent = argParent

    // 兄弟の中のインデックス
    let m_SiblingsIndex = argSiblingsIndex

    // スクロールバーの位置
    let mutable m_VScrollberPos = 0.0
    let mutable m_HScrollberPos = 0.0

    // キャッシュ再構築処理を停止するためのトークン
    let m_CacCancelToken = new Threading.CancellationTokenSource()

    do
        // 投機的に、サブフォルダの内容を読み込んでおく
        let proc =
            async {
                for i = 0 to m_SubfolderNames.Length - 1 do
                    if m_Subfolders.[i].IsNone then
                        m_Subfolders.[i] <- Some( new  FolderEntry( m_SubfolderNames.[i], Some( this ), i ) )
            }
        Async.Start( proc, m_CacCancelToken.Token )

    // 当該フォルダの配下にある画像ファイル名を取得
    member this.GetImageNames() =
        m_ImageNames

    // 当該フォルダの配下にある画像のサムネイルを、事前に読み込んでおく
    member this.PreloadMinimalImage() =
        [|
            for i = 0 to m_MinimalImages.Length - 1 do
                if m_MinimalImages.[i] = null then
                    yield async {
                        m_MinimalImages.[i] <- this.LoadMinimalImage( m_ImageNames.[i] )
                    }
        |]
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore

    // 当該フォルダの配下にある画像のサムネイルを取得する
    member this.GetMinimalImages( idx : int ) =
        if null = m_MinimalImages.[idx] then
            m_MinimalImages.[idx] <- this.LoadMinimalImage( m_ImageNames.[idx] )
        m_MinimalImages.[idx]

    // 当該フォルダの配下にあるフォルダ名を取得する
    member this.GetSubfolderNames() =
        m_SubfolderNames

    // 当該フォルダの配下にあるフォルダを取得する
    member this.GetSubfolder( fidx : int ) =
        if not ( m_Subfolders.[fidx].IsSome ) then
            m_Subfolders.[fidx] <- Some( new FolderEntry( m_SubfolderNames.[fidx], Some( this ), fidx ) )
        m_Subfolders.[fidx].Value

    // 当該フォルダの配下にあるフォルダのサムネイルを事前に読み込んでおく
    member this.PreloadSubfolderImage() =
        [|
            for i = 0 to m_SubfolderImage.Length - 1 do
                if m_SubfolderImage.[i] = null then
                    yield async {
                        let f = this.GetSubfolder( i )
                        m_SubfolderImage.[i] <- f.GetCurrentFolderSumbnail()
                    }
        |]
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore

    // 当該フォルダの配下にあるフォルダのサムネイルを取得する
    member this.GetSubfolderImage( fidx : int ) =
        if null = m_SubfolderImage.[fidx] then
            let f = this.GetSubfolder( fidx )
            m_SubfolderImage.[fidx] <- f.GetCurrentFolderSumbnail()
        m_SubfolderImage.[fidx]

    // 当該フォルダの配下にあるフォルダのサムネイルのファイル名を取得する
    member this.GetSubfolderImageName( fidx : int ) =
        if null = m_SubfolderImageName.[fidx] then
            let f = this.GetSubfolder( fidx )
            m_SubfolderImageName.[fidx] <- f.GetCurrentFolderSumbnailName()
        m_SubfolderImageName.[fidx]

    // このフォルダを現すサムネイルを取得する
    member this.GetCurrentFolderSumbnail() =
        // このフォルダに画像が1つ以上存在するのであれば、それを応答する
        if m_ImageNames.Length > 0 then
            [|
                for i in [| 0 .. Math.Min( m_ImageNames.Length, 3 ) - 1 |] ->
                    this.GetMinimalImages( i )
            |]
        elif m_SubfolderNames.Length > 0 then
            // サブフォルダが1つ以上存在するのであれば、
            // 先頭のフォルダに問い合わせる
            this.GetSubfolder( 0 ).GetCurrentFolderSumbnail()
        else
            // 応答できるものがないため、長さ0の配列を応答する
            [||]

    // このフォルダを現すサムネイルを取得する
    member this.GetCurrentFolderSumbnailName() =
        // このフォルダに画像が1つ以上存在するのであれば、それを応答する
        if m_ImageNames.Length > 0 then
            m_ImageNames.[ 0 .. Math.Min( m_ImageNames.Length, 3 ) - 1 ]
        elif m_SubfolderNames.Length > 0 then
            // サブフォルダが1つ以上存在するのであれば、
            // 先頭のフォルダに問い合わせる
            this.GetSubfolder( 0 ).GetCurrentFolderSumbnailName()
        else
            // 応答できるものがないため、長さ0の配列を応答する
            [||]

    // 親フォルダを取得する
    member this.GetParent() =
        argParent

    // 兄弟の内でのインデックスを取得する
    member this.GetSiblingsIndex() =
        m_SiblingsIndex

    

    // 縮小していない、ファイルの画像を取得する
    member this.LoadFileImage( idx : int ) =
        new BitmapImage( new Uri( m_ImageNames.[idx] ) )

    // 名前を取得する
    member this.GetName() =
        m_TargetFolder

    // 縮小版の画像を取得する
    member this.LoadMinimalImage ( file : string ) =
        // キャッシュのファイル名を得る
        let cacheFileName = this.GetCacheFileName file

        // キャッシュの構築を行う
        this.UpdateMinimalImage file |> ignore

        // キャッシュを読み込む
        let bi =
            try
                this.LoadSingleBitmap( cacheFileName )
            with
            | _ ->
                // 読み込みに失敗した場合は、ファイルを削除してもう一度作り直す
                File.Delete cacheFileName
                this.LoadMinimalImage( file )
        bi

    // キャッシュを更新する。新規にキャッシュを更新した場合は真を返す
    member this.UpdateMinimalImage( file ) =
        // キャッシュのファイル名を得る
        let cacheFileName = this.GetCacheFileName file

        // キャッシュファイルを格納するフォルダを作成する
        Directory.CreateDirectory <| Path.GetDirectoryName cacheFileName |> ignore

        if ( File.Exists cacheFileName ) then
            // 最終アクセス時間を更新する
            File.SetLastAccessTime( cacheFileName, DateTime.Now )
            false
        else
            // キャッシュが存在しないのなら、新規に作る
            let rSourceBmp = this.LoadSingleBitmap( file )
            let SourceWidth = float rSourceBmp.Width
            let SourceHeight = float rSourceBmp.Height
            let raito = Math.Sqrt( 25000.0 / ( SourceWidth * SourceHeight ) )

            // イメージを縮小する
            let t = new TransformedBitmap( rSourceBmp, new ScaleTransform( raito, raito ) )
            t.Freeze()

            // 縮小されたイメージを保存する
            let f = BitmapFrame.Create( t )
            let encoder  = new JpegBitmapEncoder()
            encoder.Frames.Add( f )
            use fs = new FileStream( cacheFileName, FileMode.Create )
            encoder.Save fs
            fs.Dispose()

            true

    // キャッシュのファイル名を得る
    member this.GetCacheFileName( fname : string ) =
        // ファイル名のハッシュ値を得る
        let hash : string = this.CalcualteHashValue( fname )

        // キャッシュフォルダのパスを得る
        let path =
            let wp = Configure.GetCachePath()
            if wp.Length = 0 then
                Path.GetTempPath() + "\\LilyViewerCache\\"
            else
                wp + "\\"

        // キャッシュファイル名を生成する
        path + hash.Substring( 0, 2 ) + "\\" + hash.Substring( 2, 2 ) + "\\" + hash + ".jpg"
    
    // ビットマップを読み込む
    member this.LoadSingleBitmap ( fname : string ) : BitmapImage =

        let wbi = new BitmapImage()
        wbi.BeginInit()
        wbi.StreamSource <-
            let wms = new MemoryStream()
            use infile = File.OpenRead( fname )
            infile.CopyTo( wms )
            wms.Seek( 0L, SeekOrigin.Begin ) |> ignore
            wms
        wbi.EndInit()
        wbi.Freeze()
        wbi


    // スクロールバーの位置を保存する
    member this.SetScrollberPos( v : float ) ( h : float ) =
        m_VScrollberPos <- v
        m_HScrollberPos <- h

    // スクロールバーの位置を取得する
    member this.GetScrollberPos() =
        ( m_VScrollberPos, m_HScrollberPos )

    // ハッシュ値を求める
    member
        private this.CalcualteHashValue( argstr : string ) =
            let hashBytes =
                ( new Cryptography.SHA1Managed() )
                    .ComputeHash( Encoding.UTF8.GetBytes( argstr ) )
            let sb = new StringBuilder( hashBytes.Length * 2 )
            hashBytes
            |> Seq.iter
                ( fun b ->
                    sb.Append( sprintf "%02X" b ) |> ignore
                )
            sb.ToString()

    // 全てのファイルについて、キャッシュの構築を行う
    member this.CreateAllCache() =

        let cacProc =
            async {
                // 自フォルダ内のキャッシュを作る
                for i = 0 to m_ImageNames.Length - 1 do
                    if m_MinimalImages.[i] = null then
                        try
                            if this.UpdateMinimalImage( m_ImageNames.[i] ) then
                                do! Async.Sleep 10
                            else
                                do! Async.Sleep 0
                        with
                        | _ -> ()
        
                // サブフォルダに対して、キャッシュの構築を指示する
                for i = 0 to m_SubfolderNames.Length - 1 do
                    this.GetSubfolder( i ).CreateAllCache()
            }
        // ルートであれば、非同期的に実行する。それ以外の場合は同期的に行う
        try
            if m_Parent.IsSome then
                Async.RunSynchronously( cacProc, -1, m_CacCancelToken.Token )
            else
                Async.Start( cacProc, m_CacCancelToken.Token )
        with
        | _ -> ()

    // キャッシュ再構築処理を停止する
    member this.StopCreateAllCacheProc() =
        // 自オブジェクト内のキャッシュ再構築処理を停止する
        m_CacCancelToken.Cancel()

        // サブフォルダに通知する
        for i = 0 to m_Subfolders.Length - 1 do
            if m_Subfolders.[i].IsSome then
                m_Subfolders.[i].Value.StopCreateAllCacheProc()

    // ルートのオブジェクトを取得する
    member this.GetRoot() =
        if m_Parent.IsNone then
            // 自分自身がルートであれば、自分自身を結果として応答する
            this
        else
            // 親に問い合わせる
            m_Parent.Value.GetRoot()


    
    