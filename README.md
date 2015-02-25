# FileSync
FileSync with 2WaySync and UNC-Path Support

```cs
namespace Demo
{
    class Program
    {
        protected static string folder1 = @"E:\test\";
        protected static string folder2 = @"E:\test2\";

        static void Main(string[] args)
        {
            QXS.FileSync.FileSync sync = new QXS.FileSync.FileSync(folder1, folder2);
            sync.AttachLogger(new QXS.FileSync.ConsoleOutputLogger());
            sync.Start(true); // start in master/master mode (2 way sync)
            
            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;

            sync.Stop();
        }
    }
}
```
