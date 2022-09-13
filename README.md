# DbBackupRestore

SQL Server データベースの完全バックアップ、および新しい場所への復元を行うツールです。

## データベースの完全バックアップ

[構成ファイル](#構成ファイル) に定義された各データベースの完全バックアップをバックアップファイルとして作成します。

* 実行例
    ```ps1
    .\BackupSqlDatabase.exe
    ```
* 実行されるSQL例
  ```sql
  BACKUP DATABASE @databaseName
    TO DISK = @backupFilePath WITH NOFORMAT
  , NAME = @description
  , INIT
  , SKIP
  , NOREWIND
  , NOUNLOAD
  , STATS = 10;
  ```

## データベースの新しい場所への復元

[構成ファイル](#構成ファイル) に定義された各データベースをバックアップファイルより定義された場所へ復元します。

* 実行例
    ```ps1
    .\RestoreSqlDatabase.exe
    ```
* 実行されるSQL例
  ```sql
  RESTORE FILELISTONLY FROM DISK = @backupFilePath;

  ALTER DATABASE @databaseName SET OFFLINE WITH ROLLBACK IMMEDIATE;

  RESTORE DATABASE @databaseName
    FROM DISK = @backupFilePath WITH REPLACE
  , NOUNLOAD
  , STATS = 5
  , MOVE N'{logicalName1}' TO N'{moveToFilePath1}'
  , MOVE N'{logicalName2}' TO N'{moveToFilePath2}';

  ALTER DATABASE @databaseName SET ONLINE;
  ```

## バックアップ先と復元先フォルダのアクセス権について
バックアップ先と復元先フォルダには SQL Server に対してアクセス許可を与える必要があります。

両ツールを実行するとそれぞれ対象となるフォルダ対して `NT Service\MSSQL$インスタンス名` にフルコントロールのアクセス権を付与します。

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
    "ArchiveDirectory": "\\\\fileserver\\share\\backup",
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
    "ArchiveDirectory": "\\\\fileserver\\share\\backup",
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

|要素名|説明|設定例|
|--|--|--|
|ConnectionString|接続文字列|`Data Source=localhost;Integrated Security=True`|
|SqlServerAccount|SQL Server サービスアカウント|`MSSQL$SQLEXPRESS`|
|BackupDirectory|`*.bak` の格納先フォルダ|`C:\\DB\\BACKUP`<br>ローカルフォルダのみ|
|RestoreDirectory|`*.bak` の復元先フォルダ|`C:\\DB\\RESTORE`<br>ローカルフォルダのみ<br>要アクセス権|
|ArchiveDirectory|`*.zip` の格納先フォルダ|`\\\\fileserver\\share\\backup`|
|CommandTimeoutSeconds|コマンドタイムアウト秒|`60`<br>既定値は`60`秒|
|Databases|データベース設定の配列|
|Databases:Name|データベース名|`DB1`|
