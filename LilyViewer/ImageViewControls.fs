namespace LilyViewer

open System
open System.Windows
open System.Windows.Controls
open System.IO
open System.Windows.Media.Imaging
open ViewConstants

type ImageViewControls =

    // 現在表示しているスライドのインデックス
    val m_CurrentIndex : int

    // 表示しているイメージ
    val m_LargeImage1 : Image
    val m_LargeImage2 : Image
    
    // 見開き2ページか否か
    val m_TwoPage : bool

    // 配置対象となるキャンバス
    val m_Canvas : Canvas

    // コンストラクタ
    new( argCanvas : Canvas, argFolder : FolderEntry, argIndx : int ) =

        let twoPageFlg = Configure.GetTwoPageFlg()

        let largeImage1 = new Image()
        let largeImage2 = new Image()

        // 表示する画像を取得する
        let imageNames = argFolder.GetImageNames()
        let currentImageNames = imageNames.[ argIndx ]

        // 表示するインデックスを確定する
        let widx1 = Math.Max( 0, Math.Min( argIndx, imageNames.Length - if twoPageFlg then 2 else 1 ) )
        let widx2 = widx1 + 1

        // 1ページ目の読み込み
        largeImage1.Source <- argFolder.LoadFileImage( widx1 )

        // 2ページ目の読み込み
        if twoPageFlg && widx2 < imageNames.Length then
            largeImage2.Source <- argFolder.LoadFileImage( widx2 )

        {
            m_CurrentIndex = argIndx;
            m_LargeImage1 = largeImage1;
            m_LargeImage2 = largeImage2;
            m_TwoPage = twoPageFlg;
            m_Canvas = argCanvas;
        }

    // サイズ変更
    interface IAddedControls with
        override this.OnSize ( wndSizeX : float ) ( wndSizeY : float ) ( updateOnly : bool ) =

            let pageDirection = Configure.GetPageDirection()
            let h = wndSizeY - 0.0
            let w =
                if this.m_TwoPage then
                    ( wndSizeX - 00.0 ) / 2.0
                else
                    wndSizeX - 00.0
            
            // 元の画像のサイズ
            let orgHeight1 = this.m_LargeImage1.Source.Height
            let orgWidth1 = this.m_LargeImage1.Source.Width

            // 拡大率を求める
            let r = Math.Min( h / orgHeight1, w / orgWidth1 );

            // 画像のサイズを設定する
            this.m_LargeImage1.Height <- orgHeight1 * r
            this.m_LargeImage1.Width <- orgWidth1 * r

            if this.m_TwoPage then
                // 元の画像のサイズ
                let orgHeight2 = this.m_LargeImage2.Source.Height
                let orgWidth2 = this.m_LargeImage2.Source.Width

                // 拡大率を求める
                let r2 = Math.Min( h / orgHeight2, w / orgWidth2 )

                // 画像のサイズを設定する
                this.m_LargeImage2.Height <- orgHeight2 * r2
                this.m_LargeImage2.Width <- orgWidth2 * r2

            if not this.m_TwoPage then
                // 1ページだけ表示する場合は、画面の真ん中に表示する
                Canvas.SetLeft( this.m_LargeImage1, w / 2.0 - orgWidth1 * r / 2.0 )
                Canvas.SetTop( this.m_LargeImage1, ( h / 2.0 ) - ( orgHeight1 * r / 2.0 ) )
            else
                // 見開き2ページの場合

                if pageDirection then
                    // 右から左の場合

                    // 1ページ目を右側に表示する
                    Canvas.SetLeft( this.m_LargeImage1, w )
                    Canvas.SetTop( this.m_LargeImage1, h / 2.0 - this.m_LargeImage1.Height / 2.0 )

                    // 2ページ目を左側に表示する
                    Canvas.SetLeft( this.m_LargeImage2, w - this.m_LargeImage2.Width )
                    Canvas.SetTop( this.m_LargeImage2, h / 2.0 - this.m_LargeImage2.Height / 2.0 )
                else
                    // 左から右の場合

                    // 1ページ目を右側に表示する
                    Canvas.SetLeft( this.m_LargeImage1, w - this.m_LargeImage1.Width )
                    Canvas.SetTop( this.m_LargeImage1, h / 2.0 - this.m_LargeImage1.Height / 2.0 )

                    // 2ページ目を左側に表示する
                    Canvas.SetLeft( this.m_LargeImage2, w )
                    Canvas.SetTop( this.m_LargeImage2, h / 2.0 - this.m_LargeImage2.Height / 2.0 )

            if not updateOnly then
                try
                    this.m_Canvas.Children.Add( this.m_LargeImage1 ) |> ignore
                    this.m_Canvas.Children.Add( this.m_LargeImage2 ) |> ignore
                with
                | _ -> ()

            // キャンバスのサイズを指定する
            if not this.m_TwoPage then
                this.m_Canvas.Width <- w
                this.m_Canvas.Height <- h
            else
                this.m_Canvas.Width <- w * 2.0
                this.m_Canvas.Height <- h

        // 表示している画面の種別を応答する
        override this.GetViewType() =
            1

    // 現在表示しているページを取得
    member this.GetPageIndex() =
        this.m_CurrentIndex