using System;

namespace NSW.Logging.Files
{
    public class FileLogColumn
    {
        public string Name { get; internal set; }
        public int Size { get; internal set; }
        public Func<LogEntry, string> Formatter { get; internal set; }

        internal FileLogColumn(){}
    }
}