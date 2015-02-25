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
            // folder1 = source, folder2 = destination
            // notice: in 2 way sync folder1 = source/destination and folder2 = source/destination
            QXS.FileSync.FileSync sync = new QXS.FileSync.FileSync(folder1, folder2);
            
            // output every action on the destination side to the console
            sync.AttachLogger(new QXS.FileSync.ConsoleOutputLogger());
            
            // start in master/master mode (2 way sync)
            sync.Start(true);  // set it to false, for master/slave mode (1 way sync)
            
            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;

            sync.Stop();
        }
    }
}
```
