using System.Data.SQLite;

namespace DBTest
{
    // DBチェック用
    public class Program
    {
        // マシンクラス
        public class Machine
        {
            // マシン名
            public string Name { get; set; } = string.Empty;
            public string ModelInfoId { get; set; } = string.Empty;
            public List<string> PrintConditionId { get; set; } = new List<string>();

            // コンストラクタ
            public Machine(string name)
            {
                Name = name;
                // ModelInfoIdとPrintConditionIdを取得、取得出来なかったら例外を投げる
                if ( !GetModelInfoId(name))
                    throw new Exception("ModelInfoId not found");
                if (!GetPrintConditionId())
                    throw new Exception("PrintConditionId not found");
            }

            // ModelInfoIdを取得するメソッド
            private bool GetModelInfoId(string name)
            {
                try
                {
                    // データベースのパスを設定ModelInformation.db、
                    // DBのパスはプロジェクトのルートディレクトリのdbフォルダにある
                    // ex:C:\Users\yichi.zhang\source\repos\DBTest\db
                    // 今はgit管理外のためなければ手動で作成する必要がある
                    string dbPath = Path.Combine(
                        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..")),
                        "db", "ModelInformation.db"
                    );
                    // DBからマシン名からModelInfoIdを取得する
                    // ex：UJF-3042MkII であれば、060509を取得する
                    SQLiteConnectionStringBuilder connection = new SQLiteConnectionStringBuilder
                    {
                        DataSource = dbPath
                    };
                    using (SQLiteConnection conn = new SQLiteConnection(connection.ToString()))
                    {
                        conn.Open();
                        
                        using (var command = new SQLiteCommand(conn))
                        {
                            command.CommandText = "SELECT ModelInfoId FROM Model WHERE Memo = '" + name + "';";

                            ModelInfoId = (string)command.ExecuteScalar();
                        }
                        conn.Close();
                    }
                    if (string.IsNullOrWhiteSpace(ModelInfoId))
                        return false;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }

            // PrintConditionIdを取得するメソッド
            private bool GetPrintConditionId()
            {
                try
                {

                    // データベースのパスを設定ModelInfoParameter.db、
                    // DBのパスはプロジェクトのルートディレクトリのdbフォルダにある
                    // ex:C:\Users\yichi.zhang\source\repos\DBTest\db
                    // 今はgit管理外のためなければ手動で作成する必要がある
                    string dbPath = Path.Combine(
                        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..")),
                        "db", "ModelInfoParameter.db"
                    );


                    // DBからModelInfoIdからPrintConditionIdを取得する
                    // ex：UJF-3042MkII であれば、10, 130, 140を取得する
                    // !!! 99はいらないため、取得しません !!!
                    SQLiteConnectionStringBuilder connection = new SQLiteConnectionStringBuilder
                    {
                        DataSource = dbPath,
                    };
                    using (SQLiteConnection conn = new SQLiteConnection(connection.ToString()))
                    {
                        conn.Open();

                        using (var command = new SQLiteCommand(conn))
                        {
                            command.CommandText = "SELECT PrintConditionId FROM PrintCondition WHERE ModelInfoId = '" + ModelInfoId + "' AND Version != '99';";

                            using var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                if (reader["PrintConditionId"].ToString() != null)
                                    PrintConditionId.Add(reader["PrintConditionId"].ToString());
                            }
                        }

                        conn.Close();
                    }
                    if(PrintConditionId.Count == 0)
                        return false;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }
        public static List<Machine> MachineList { get; set; } = new List<Machine>();

        // 取得するインクセットコード
        public static List<(string name, string code)> InkSetCode { get; set; } = new List<(string name, string code)>()
        {

            //("LH-100_4C", "0010220901"),
            //("LH-100_6C", "0020220901"),
            //("ELH-100_4C", "0010220905"),
            //("ELH-100_6C", "0020220905"),
            ("LUS-120_4C", "0010220503"),
            ("LUS-120_6C", "0020220503"),
            ("ELS-120_4C", "0010220507"),
            ("ELS-120_6C", "0020220507")
        };
        public static void Main(string[] args)
        {
            // マシン名のリスト
            var List = new List<string>()
            {
                "UJF-3042MkII", "UJF-3042MkII e", "UJF3042MkII EX", "UJF-3042MkII EX e",
                "UJF-6042MkII", "UJF-6042MkII e",
                "UJF-A3MkII","UJF-A3MkII EX",
                //"UJF7151 Plus","UJF-7151plus II"
            };
            //var List = new List<string>()
            //{
            //    "JFX200EX","JFX200-1213EX"
            //};

            // マシン名のリストをループして、Machineクラスのインスタンスを作成
            foreach (var item in List)
            {
                Console.WriteLine(item+ "\n");
                try
                {
                    MachineList.Add(new Machine(item));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // MachineListの中身を確認
            foreach (var machine in MachineList)
            {
                // 出力ファイルパス
                // ex：C:\Users\yichi.zhang\source\repos\DBTest\UJF-7151plus II\UJF-7151plus II_29_LH-100_4C_PrintCondition_output.txt
                string path = Path.Combine(AppContext.BaseDirectory, @"..\..\..", machine.Name);

                DirectoryInfo di = new DirectoryInfo(path);
                if (!di.Exists)
                {
                    di.Create();
                }
                else
                {
                    foreach (var file in di.GetFiles())
                    {
                        file.Delete();
                    }
                }
                CreateDBResult(di.FullName, machine);
            }
        }

        private static void CreateDBResult(string path,Machine machine)
        {
            foreach (var PrintConditionId in machine.PrintConditionId)
            {
                foreach (var inkSetCode in InkSetCode)
                {
                    // ファイル名例：UJF-7151plus II_29_LH-100_4C_PrintCondition_output.txt
                    if (GetDBData(Path.Combine(path,(machine.Name + "_" + PrintConditionId + "_" + inkSetCode.name)), PrintConditionId, inkSetCode.code))
                    {
                        Console.WriteLine("Machine : " + machine.Name + " PrintConditionId : " + PrintConditionId + " InkSetName: " + inkSetCode.name + " InkSetCode: " + inkSetCode.code);
                    }
                }
            }
        }

        // PrintConditionIdとInkSetCodeでDBデータを探し、ファイルへ出力する
        public static bool GetDBData(string filename, string PrintConditionId , string InkSetCode)
        {
            try
            {
                string dbPath = Path.Combine(
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..")),
                    "db", "ModelInfoParameter.db"
                );
                SQLiteConnectionStringBuilder connection = new SQLiteConnectionStringBuilder
                {
                    DataSource = dbPath,
                };
                using (SQLiteConnection conn = new SQLiteConnection(connection.ToString()))
                {
                    conn.Open();
                    using (var command = new SQLiteCommand(conn))
                    {
                        command.CommandText = @"SELECT * FROM PrintConditionData WHERE PrintConditionId = '" + PrintConditionId+ "' AND InkSetCode = '" + InkSetCode + "';";
                        using var reader = command.ExecuteReader();
                        if (!reader.HasRows)
                        {
                            conn.Close();
                            return false;
                        }
                        int fieldCount = reader.FieldCount;
                        using var writer = new StreamWriter(filename + "_PrintCondition_output.txt");

                        while (reader.Read())
                        {
                            for (int i = 2; i < fieldCount; i++)
                            {
                                writer.Write(reader.GetName(i) + ": " + reader.GetValue(i) + "\t");
                            }
                            writer.WriteLine();
                        }
                    }
                    conn.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}