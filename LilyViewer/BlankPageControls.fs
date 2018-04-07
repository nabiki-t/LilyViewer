namespace LilyViewer

open System
open System.Windows
open System.Windows.Controls
open System.IO
open System.Windows.Media.Imaging
open ViewConstants

type BlankPageControls =

    // 配置対象となるキャンバス
    val m_Canvas : Canvas

    // コンストラクタ
    new( argCanvas : Canvas ) =
        {
            m_Canvas = argCanvas;
        }

    // サイズ変更
    interface IAddedControls with
        override this.OnSize ( wndSizeX : float ) ( wndSizeY : float ) ( updateOnly : bool ) =
            this.m_Canvas.Width <- float wndSizeX
            this.m_Canvas.Height <- float wndSizeY

        // 表示している画面の種別を応答する
        override this.GetViewType() =
            2
