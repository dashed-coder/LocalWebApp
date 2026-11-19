# LocalWebApp

`wwwroot` に配置した静的な Web アプリを、Windows デスクトップ上で実行するためのアプリです。HTML、CSS、JavaScript などで作成した Web アプリを、WebView2 を使ってウィンドウ内に表示します。

## 使い方

```text
├─ LocalWebApp.exe
└─ wwwroot/
   ├─ index.html
   └─ ...
```

`wwwroot/` に実行したい静的な Web アプリを配置してLocalWebApp.exeを起動してください。

## Web アプリの差し替え・更新

アプリを終了してから、配布フォルダー内の `wwwroot` の内容を差し替えてください。差し替え後、exe を再度起動すると更新した内容が表示されます。アプリ本体の再ビルドは不要です。

## 動作環境

- Windows
- Microsoft Edge WebView2 Runtime
- .NET Desktop Runtime

WebView2 Runtime がインストールされていない場合は、インストールしてから起動してください。

## 動作上の注意

- このアプリは Web サーバーではなく、`wwwroot` 内の静的ファイルを表示します。サーバー側のプログラムや API は実行しません。
- `wwwroot/index.html` が見つからない場合は起動できません。
- アプリのウィンドウは WebView2 を使ったもので、Web アプリを Windows のネイティブアプリに変換するものではありません。
