using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;

namespace LocalWebApp;

public partial class MainWindow : Window
{
    private CancellationTokenSource? initializationCTS;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
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

        initializationCTS = new CancellationTokenSource();
        var cancellationToken = initializationCTS.Token;

        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalWebApp",
                "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            cancellationToken.ThrowIfCancellationRequested();

            await Browser.EnsureCoreWebView2Async(environment);
            cancellationToken.ThrowIfCancellationRequested();

            Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "appassets.example",
                wwwroot,
                CoreWebView2HostResourceAccessKind.DenyCors);

            await Browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                """
                if (location.origin === "https://appassets.example" && location.pathname === "/index.html") {
                    history.replaceState(history.state, "", "/" + location.search + location.hash);
                }
                """);
            cancellationToken.ThrowIfCancellationRequested();

            Browser.CoreWebView2.NavigationCompleted += Browser_NavigationCompleted;
            Browser.CoreWebView2.Navigate("https://appassets.example/index.html");
        }
        catch (OperationCanceledException)
        {
            // ウィンドウ閉じられた
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
