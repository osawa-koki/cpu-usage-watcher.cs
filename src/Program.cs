#pragma warning disable CA1416

using System.Security.Cryptography;
using System.Diagnostics;

class Program
{
  private static PerformanceCounter cpuCounter = new("Processor", "% Processor Time", "_Total");
  static void Main()
  {
    // サブタスクを作成し、処理を開始する
    Task subTask = Task.Run(DoWork);

    while (true)
    {
      // CPU使用率を取得する
      var cpuUsage = GetTotalCpuUsage();

      Console.WriteLine("CPU : {0:f}%", cpuUsage);

      // CPU使用率が50%を上回った場合、サブタスクの処理を中断する
      if (cpuUsage > 50)
      {
        Console.WriteLine("Sub-task is stopping temporarily due to high CPU usage.");
        subTask.Wait(100);
        Task.Delay(100).Wait();
        Console.WriteLine("Sub-task restarted.");
      }

      // サブタスクが終了したら抜ける
      if (subTask.IsCompleted)
      {
        Console.WriteLine("Sub-task completed.");
        break;
      }

      // 1秒待機する
      Task.Delay(3).Wait();
    }
  }

  static void DoWork()
  {
    // サブタスクで行う処理を記述する

    // 1MBのバイト配列を作成
    var buffer = new byte[1024 * 1024 * 1024];

    // バイト配列をランダムに埋める
    var random = new Random();
    random.NextBytes(buffer);

    // バイト配列のSHA256ハッシュ値を計算
    var hashed = SHA512.HashData(buffer);
    Console.WriteLine($"SHA512 -> {BitConverter.ToString(hashed)}");
  }

  static float GetTotalCpuUsage()
  {
    // 現在の全体のCPU使用率を取得する
    return cpuCounter.NextValue();
  }
}

#pragma warning restore CA1416
