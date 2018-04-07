namespace LilyViewer

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Media.Imaging

type ConfigWindow() =
    // XAMLのコードをリソースからロードする
    let m_Window =
        Application.LoadComponent(
            new System.Uri( "/LilyViewer;component/ConfigWindow.xaml", UriKind.Relative )
        ) :?> Window
    
    let m_FolderPathText = m_Window.FindName( "FolderPathText" ) :?> TextBox
    let m_FolderPathBrsBtn = m_Window.FindName( "FolderPathBrsBtn" ) :?> Button
    let m_CachePathText = m_Window.FindName( "CachePathText" ) :?> TextBox
    let m_CachePathBrsBtn = m_Window.FindName( "CachePathBrsBtn" ) :?> Button
    let m_PageDirectionCombo = m_Window.FindName( "PageDirectionCombo" ) :?> ComboBox
    let m_SumbnailSizeCombo = m_Window.FindName( "SumbnailSizeCombo" ) :?> ComboBox
    let m_CacheLifetimeCombo = m_Window.FindName( "CacheLifetimeCombo" ) :?> ComboBox
    let m_OkButton = m_Window.FindName( "OkButton" ) :?> Button
    let m_CancelButton = m_Window.FindName( "CancelButton" ) :?> Button

    // 初期化
    member this.Initialize ( argOwner : Window ) =
        m_CachePathBrsBtn.Click.AddHandler ( fun sender e -> this.CachePathBrsBtn_Click sender e )
        m_FolderPathBrsBtn.Click.AddHandler ( fun sender e -> this.FolderPathBrsBtn_Click sender e )
        m_OkButton.Click.AddHandler ( fun sender e -> this.OKButton_Click sender e )
        m_CancelButton.Click.AddHandler ( fun sender e -> this.CancelButton_Click sender e )

        m_CachePathText.Text <- Configure.GetCachePath()
        m_FolderPathText.Text <- Configure.GetFolderPath()
        if Configure.GetPageDirection() then
            m_PageDirectionCombo.SelectedIndex <- 0
        else
            m_PageDirectionCombo.SelectedIndex <- 1
        m_SumbnailSizeCombo.SelectedIndex <- Configure.GetSumbnailSize()
        m_CacheLifetimeCombo.SelectedIndex <- Configure.GetCashLifetime()

    
    // 表示
    member this.Show () =
        m_Window.ShowDialog().GetValueOrDefault( false )



    // OKボタンの押下
    member private this.OKButton_Click sender e =
        // 入力値を取得する
        Configure.SetCachePath( m_CachePathText.Text )
        Configure.SetFolderPath( m_FolderPathText.Text )
        if m_PageDirectionCombo.SelectedIndex = 0 then
            Configure.SetPageDirection true
        else
            Configure.SetPageDirection false
        Configure.SetSumbnailSize( m_SumbnailSizeCombo.SelectedIndex )
        Configure.SetCashLifetime( m_CacheLifetimeCombo.SelectedIndex )

        m_Window.DialogResult <- Nullable<bool>( true )
        m_Window.Close ()

    // Cancelボタンの押下
    member private this.CancelButton_Click sender e =
        // 保存せずに閉じる
        m_Window.Close ()

    
    // フォルダの参照ボタンの押下
    member private this.FolderPathBrsBtn_Click sender e =
        let d = new System.Windows.Forms.FolderBrowserDialog()
        let r = d.ShowDialog()
        if r = System.Windows.Forms.DialogResult.OK then
            m_FolderPathText.Text <- d.SelectedPath

    // キャッシュ格納先パスの参照ボタンの押下
    member private this.CachePathBrsBtn_Click sender e =
        let d = new System.Windows.Forms.FolderBrowserDialog()
        let r = d.ShowDialog()
        if r = System.Windows.Forms.DialogResult.OK then
            m_CachePathText.Text <- d.SelectedPath

