using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using translator.Libraries;

namespace translator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    Reader.Initialize(openFileDialog.FileName);

                    StringBuilder fileContent = new StringBuilder();
                    while (!Reader.IsEndOfFile())
                    {
                        fileContent.AppendLine($"Строка: {Reader.LineNumber}, Позиция: {Reader.SymbolPositionInLine}, Символ: {(char)Reader.CurrentSymbol}");
                        Reader.ReadNextSymbol();
                    }

                    SourceTextBox.Document.Blocks.Clear();
                    SourceTextBox.Document.Blocks.Add(new Paragraph(new Run(fileContent.ToString())));

                    MessageTextBox.Document.Blocks.Clear();
                    MessageTextBox.Document.Blocks.Add(new Paragraph(new Run("Файл успешно загружен и прочитан")));
                }
                catch (Exception ex)
                {
                    MessageTextBox.Document.Blocks.Clear();
                    MessageTextBox.Document.Blocks.Add(new Paragraph(new Run($"Ошибка при чтении файла: {ex.Message}")));
                }
                finally
                {
                    Reader.Close();
                }
            }
        }
        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                TextRange textRange = new TextRange(ResultTextBox.Document.ContentStart, ResultTextBox.Document.ContentEnd);
                File.WriteAllText(saveFileDialog.FileName, textRange.Text);
            }
        }

        private void Compile_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                TextRange sourceTextRange = new TextRange(SourceTextBox.Document.ContentStart, SourceTextBox.Document.ContentEnd);
                string sourceCode = sourceTextRange.Text;

                ResultTextBox.Document.Blocks.Clear();
                ResultTextBox.Document.Blocks.Add(new Paragraph(new Run("Результат компиляции")));

                MessageTextBox.Document.Blocks.Clear();
                MessageTextBox.Document.Blocks.Add(new Paragraph(new Run("Компиляция выполнена успешно")));
            }
            catch (Exception ex)
            {
                MessageTextBox.Document.Blocks.Clear();
                MessageTextBox.Document.Blocks.Add(new Paragraph(new Run($"Ошибка при компиляции: {ex.Message}")));
            }
        }
    }
}
