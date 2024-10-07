using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexical_Analyzer_Libary.Classes
{
    /// <summary>
    /// Парсер файлов
    /// </summary>
    public class Reader
    {
        private  int lineNumber;
        private  int symbolPositionInLine;
        private  int currentSymbol;
        private  StreamReader streamReader;

        public  int LineNumber => lineNumber;
        public  int SymbolPositionInLine => symbolPositionInLine;
        public  int CurrentSymbol => currentSymbol;

        public  void ReadNextSymbol()
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

        public Reader(string filePath)
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

        public  void Close()
        {
            if (streamReader != null)
            {
                streamReader.Close();
                streamReader = null;
            }
        }

        public  bool IsEndOfFile()
        {
            return currentSymbol == char.MaxValue;
        }
    }
}
