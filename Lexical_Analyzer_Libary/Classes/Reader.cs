using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace translator.Libraries
{
    /// <summary>
    /// Парсер файлов
    /// </summary>
    public static class Reader
    {
        private static int lineNumber;
        private static int symbolPositionInLine;
        private static int currentSymbol;
        private static StreamReader streamReader;

        public static int LineNumber => lineNumber;
        public static int SymbolPositionInLine => symbolPositionInLine;
        public static int CurrentSymbol => currentSymbol;

        public static void ReadNextSymbol()
        {
            currentSymbol = streamReader.Read();
            if (currentSymbol == -1)
            {
                currentSymbol = char.MaxValue; // Представляем конец файла как char.MaxValue
            }
            else if (currentSymbol == '\n')
            {
                lineNumber++;
                symbolPositionInLine = 0;
            }
            else if (currentSymbol == '\r' || currentSymbol == '\t')
            {
                ReadNextSymbol();
            }
            else
            {
                symbolPositionInLine++;
            }
        }

        public static void Initialize(string filePath)
        {
            if (File.Exists(filePath))
            {
                Close(); // Закрываем предыдущий StreamReader, если он был открыт
                streamReader = new StreamReader(filePath);
                lineNumber = 1;
                symbolPositionInLine = 0;
                ReadNextSymbol();
            }
            else
            {
                throw new FileNotFoundException("Файл не найден", filePath);
            }
        }

        public static void Close()
        {
            if (streamReader != null)
            {
                streamReader.Close();
                streamReader = null;
            }
        }

        public static bool IsEndOfFile()
        {
            return currentSymbol == char.MaxValue;
        }
    }
}
