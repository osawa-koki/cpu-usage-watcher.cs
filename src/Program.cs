#pragma warning disable CA1416

using System.Security.Cryptography;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Linq;
using System.Text;

internal class Program
{
  private static readonly PerformanceCounter cpuCounter = new("Processor", "% Processor Time", "_Total");
  internal static int Main()
  {
    // 設定ファイルの検証
    string xml_path = "./config.xml";
    string xsd_path = "./config.xsd";

    if (File.Exists(xml_path) == false)
    {
      Console.WriteLine("Could not find XML configuration file.");
      return 1;
    }
    if (File.Exists(xsd_path) == false)
    {
      Console.WriteLine("Could not find XSD configuration file.");
      return 1;
    }

    string xml_content = File.ReadAllText(xml_path);
    string xsd_content = File.ReadAllText(xsd_path);

    bool validation_check = true;

    //XMLスキーマオブジェクトの生成
    XmlSchema schema = new();
    using (StringReader stringReader = new(xsd_content))
    {
      schema = XmlSchema.Read(stringReader, null)!;
    }
    // スキーマの追加
    XmlSchemaSet schemaSet = new();
    schemaSet.Add(schema);

    // XML文書の検証を有効化
    XmlReaderSettings settings = new()
    {
      ValidationType = ValidationType.Schema,
      Schemas = schemaSet
    };
    settings.ValidationEventHandler += (object? sender, ValidationEventArgs e) => {
      if (e.Severity == XmlSeverityType.Warning)
      {
        Console.WriteLine($"Validation Warning ({e.Message})");
      }
      if (e.Severity == XmlSeverityType.Error)
      {
        Console.WriteLine($"Validation Error ({e.Message})");
        validation_check = false;
      }
    };

    // XMLデータの読み込み
    using (StringReader stringReader = new(xml_content))
    using (XmlReader xmlReader = XmlReader.Create(stringReader, settings))
    {
      while (xmlReader.Read()) { }
    }

    if (validation_check == false)
    {
      Console.WriteLine("XML Validation failed...");
      return 1;
    }

    // 設定ファイルの読み込み
    XDocument xml_document = XDocument.Parse(xml_content);
    XElement config = xml_document.Element("config")!;
    int cpu_usage_threshold_percent = int.Parse(config.Element("cpu_usage_threshold_percent")!.Value);
    int cpu_observing_interval_millisecond = int.Parse(config.Element("cpu_observing_interval_millisecond")!.Value);
    int idle_time_millisecond = int.Parse(config.Element("idle_time_millisecond")!.Value);
    TargetStruct[] targets = config.Element("targets")!.Elements("target").Select(x => new TargetStruct
    {
      value = x.Element("value")!.Value,
      algo = (Algo)Enum.Parse(typeof(Algo), x.Element("algo")!.Value)
    }).ToArray();

    // サブタスクを作成し、処理を開始する
    Task subTask = Task.Run(() => { CalcHashes(targets); });

    while (true)
    {
      // CPU使用率を取得する
      var cpuUsage = cpuCounter.NextValue();

      Console.WriteLine("CPU : {0:f}%", cpuUsage);

      // CPU使用率がCPU使用率上限を上回った場合、サブタスクの処理を中断する
      if (cpuUsage > cpu_usage_threshold_percent)
      {
        Console.WriteLine("Sub-task is stopping temporarily due to high CPU usage.");
        subTask.Wait(idle_time_millisecond);
        Task.Delay(idle_time_millisecond).Wait();
        Console.WriteLine("Sub-task restarted.");
      }

      // サブタスクが終了したら抜ける
      if (subTask.IsCompleted)
      {
        Console.WriteLine("Sub-task completed.");
        break;
      }

      // 1秒待機する
      Task.Delay(cpu_observing_interval_millisecond).Wait();
    }
    return 0;
  }

  internal static void CalcHashes(TargetStruct[] targets)
  {
    foreach (var target in targets)
    {
      string before = target.value;
      string after = target.algo switch
      {
        Algo.MD5 => CalcMD5(before),
        Algo.SHA1 => CalcSHA1(before),
        Algo.SHA256 => CalcSHA256(before),
        Algo.SHA384 => CalcSHA384(before),
        Algo.SHA512 => CalcSHA512(before),
        _ => throw new Exception("Unknown hash algo."),
      };
      Console.WriteLine($"★ {before} -> {after}");
    }
  }

  internal static string CalcMD5(string value)
  {
    var hashed = MD5.HashData(Encoding.UTF8.GetBytes(value));
    var hash_string = BitConverter.ToString(hashed).Replace("-", "");
    return hash_string;
  }

  internal static string CalcSHA1(string value)
  {
    var hashed = SHA1.HashData(Encoding.UTF8.GetBytes(value));
    var hash_string = BitConverter.ToString(hashed).Replace("-", "");
    return hash_string;
  }

  internal static string CalcSHA256(string value)
  {
    var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    var hash_string = BitConverter.ToString(hashed).Replace("-", "");
    return hash_string;
  }

  internal static string CalcSHA384(string value)
  {
    var hashed = SHA384.HashData(Encoding.UTF8.GetBytes(value));
    var hash_string = BitConverter.ToString(hashed).Replace("-", "");
    return hash_string;
  }
  internal static string CalcSHA512(string value)
  {
    var hashed = SHA512.HashData(Encoding.UTF8.GetBytes(value));
    var hash_string = BitConverter.ToString(hashed).Replace("-", "");
    return hash_string;
  }
}

internal enum Algo
{
  MD5,
  SHA1,
  SHA256,
  SHA384,
  SHA512,
}

internal struct TargetStruct
{
  internal string value;
  internal Algo algo;
}

#pragma warning restore CA1416
