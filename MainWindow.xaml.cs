using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LocalWebApp;

public partial class MainWindow : Window
{
    private CancellationTokenSource? initializationCTS;
    private int faviconRequestId;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        initializationCTS = new CancellationTokenSource();
        var cancellationToken = initializationCTS.Token;
        try
        {
            // wwwroot と index.htmlを確認
            var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var indexPath = Path.Combine(wwwroot, "index.html");

            if (!Directory.Exists(wwwroot))
            {
                ShowStartupError($"wwwroot が見つかりません。\n\n{wwwroot}");
                return;
            }
            if (!File.Exists(indexPath))
            {
                ShowStartupError($"index.html が見つかりません。\n\n{indexPath}");
                return;
            }

            // WebView2用のユーザーデータフォルダを設定
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalWebApp",
                "WebView2");

            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            cancellationToken.ThrowIfCancellationRequested();

            // WebView2を初期化
            await Browser.EnsureCoreWebView2Async(environment);
            cancellationToken.ThrowIfCancellationRequested();

            var core = Browser.CoreWebView2;
            core.SetVirtualHostNameToFolderMapping(
                "appassets.example",
                wwwroot,
                CoreWebView2HostResourceAccessKind.DenyCors);

            // React Router用のURL正規化スクリプト
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "LocalWebApp.Scripts.NormalizeUrl.js"
            ) ?? throw new InvalidOperationException("リソースが見つかりません。");
            using var reader = new StreamReader(stream);
            var script = await reader.ReadToEndAsync(cancellationToken);

            await core.AddScriptToExecuteOnDocumentCreatedAsync(script);
            cancellationToken.ThrowIfCancellationRequested();

            // WebView2の設定
            var settings = core.Settings;
            settings.AreDefaultContextMenusEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = false;
            settings.IsSwipeNavigationEnabled = false;
            settings.AreHostObjectsAllowed = false;
            settings.IsWebMessageEnabled = false;

            core.NewWindowRequested += Browser_NewWindowRequested;
            core.DocumentTitleChanged += Browser_DocumentTitleChanged;
            core.FaviconChanged += Browser_FaviconChanged;
            core.NavigationCompleted += Browser_NavigationCompleted;
            core.Navigate("https://appassets.example/index.html");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // ウィンドウが閉じられた
        }
        catch (WebView2RuntimeNotFoundException)
        {
            if (!cancellationToken.IsCancellationRequested)
                ShowStartupError("Microsoft Edge WebView2 Runtime が見つかりません。インストールしてから再実行してください。");
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
                ShowStartupError($"WebView2 の初期化に失敗しました。\n\n{ex.Message}");
        }
    }

    private void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;

        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
            return;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void Browser_DocumentTitleChanged(object? sender, object e)
    {
        Title = Browser.CoreWebView2.DocumentTitle;
    }

    private async void Browser_FaviconChanged(object? sender, object e)
    {
        var cancellation = initializationCTS;
        if (cancellation == null || cancellation.IsCancellationRequested)
            return;

        var requestId = ++faviconRequestId;

        try
        {
            using var favicon = await Browser.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            if (!cancellation.IsCancellationRequested && requestId == faviconRequestId)
                Icon = BitmapFrame.Create(favicon, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }
        catch { }
    }

    private void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        Browser.CoreWebView2.NavigationCompleted -= Browser_NavigationCompleted;
        if (!e.IsSuccess && initializationCTS?.IsCancellationRequested != true)
            ShowStartupError($"ページを表示できませんでした。\n\n{e.WebErrorStatus}");
    }

    private void ShowStartupError(string message)
    {
        MessageBox.Show(this, message, "LocalWebApp", MessageBoxButton.OK, MessageBoxImage.Error);
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        initializationCTS?.Cancel();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        initializationCTS?.Dispose();
        Browser.Dispose();
    }
}
