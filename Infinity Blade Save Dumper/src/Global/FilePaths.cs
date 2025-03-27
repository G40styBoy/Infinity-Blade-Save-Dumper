namespace SaveDumper.Globals.FilePaths
{
    public static class FilePaths
    {
        private static string input = @"SAVE\input\";
        private static string output = @"SAVE\output\";
        private static string IB1 = @"IB1";
        private static string IB2 = @"IB2";
        private static string IB3 = @"IB3";

        private static DirectoryInfo parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory())!;
        private static string testPath = Path.Combine(parentDirectory.FullName, @"SAVE\Test");


        // public static string saveFilePath = Path.Combine(parentDirectory.FullName, input);
        // public static string outputPath = Path.Combine(parentDirectory.FullName, output);

        public static string[] saveLocation = Directory.GetFiles(testPath, "*.bin");
    }
}