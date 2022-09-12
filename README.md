# DbBackupRestore

SQL Server データベースの完全バックアップ、および新しい場所への復元を行うツールです。

## データベースの完全バックアップ

[構成ファイル](#構成ファイル) に定義された各データベースの完全バックアップをバックアップファイルとして作成します。

* 実行例
    ```ps1
    .\BackupSqlDatabase.exe
    ```

## データベースの新しい場所への復元

[構成ファイル](#構成ファイル) に定義された各データベースをバックアップファイルより定義された場所へ復元します。

* 実行例
    ```ps1
    .\RestoreSqlDatabase.exe
    ```

## 復元先フォルダのアクセス権について
復元先フォルダには SQL Server に対してアクセス許可を与えておく必要があります。

両ツールを実行するとそれぞれ対象となるフォルダのアクセス権に `NT Service\MSSQL$インスタンス名` をフルコントロールを付与します。

[構成ファイル](#構成ファイル) の 'SqlServerAccount' 設定値が `MSSQL$SQLEXPRESS` であれば `NT Service\MSSQL$SQLEXPRESS` にフルコントロールを付与します。


## 構成ファイル

データベースの完全バックアップ、およびおよび新しい場所への復元の両ツールで同一の構成ファイルを使用します。

```json
// appsettings.json
{
  // ...
  "SqlDatabaseOptions": {
    "ConnectionString": "Data Source=localhost;Integrated Security=True",
    "SqlServerAccount": "MSSQL$SQLEXPRESS",
    "BackupDirectory": "C:\\DB\\BACKUP",
    "RestoreDirectory": "C:\\DB\\RESTORE",
    "CommandTimeoutSeconds": 60,
    "Databases": [
      { "Name": "DB1" },
      { "Name": "DB2" },
      { "Name": "DB3" }
    ]
  },
  // ...
}
```

<details>
<summary>appsettings.json の完全な例:</summary>
<div>

```json
{
  "SqlDatabaseOptions": {
    "ConnectionString": "Data Source=localhost;Integrated Security=True",
    "SqlServerAccount": "MSSQL$SQLEXPRESS",
    "BackupDirectory": "C:\\DB\\BACKUP",
    "RestoreDirectory": "C:\\DB\\RESTORE",
    "CommandTimeoutSeconds": 60,
    "Databases": [
      { "Name": "DB1" },
      { "Name": "DB2" },
      { "Name": "DB3" }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

</div>
</details>

|要素名|説明|設定例|備考|
|--|--|--|--|
|ConnectionString|接続文字列|`Data Source=localhost;Integrated Security=True`||
|SqlServerAccount|SQL Server サービスアカウント|`MSSQL$SQLEXPRESS`|既定値は`MSSQL$SQLEXPRESS`|
|BackupDirectory|`*.bak` の格納先フォルダ|`C:\\DB\\BACKUP`|ローカルフォルダのみ|
|RestoreDirectory|`*.bak` の復元先フォルダ|`C:\\DB\\RESTORE`|ローカルフォルダのみ、要アクセス権|
|CommandTimeoutSeconds|コマンドタイムアウト秒|60|既定値は60秒|
|Databases|データベース設定の配列||
|Databases:Name|データベース名|`DB1`|
