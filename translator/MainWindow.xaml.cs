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
using Lexical_Analyzer_Libary.Classes;

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
                Reader _reader = new Reader(openFileDialog.FileName);
                try
                {
                    StringBuilder fileContent = new StringBuilder();
                    while (!_reader.IsEndOfFile())
                    {
                        fileContent.Append((char)_reader.CurrentSymbol); // просто добавляем символ
                        _reader.ReadNextSymbol();
                    }

                    SourceTextBox.Document.Blocks.Clear();
                    SourceTextBox.Document.Blocks.Add(new Paragraph(new Run(fileContent.ToString())));

                    MessageTextBox.Document.Blocks.Clear();
                    Paragraph successParagraph = new Paragraph(new Run("Файл успешно загружен и прочитан"))
                    {
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Black // Черный цвет для успешного сообщения
                    };
                    MessageTextBox.Document.Blocks.Add(successParagraph);
                }
                catch (Exception ex)
                {
                    MessageTextBox.Document.Blocks.Clear();
                    MessageTextBox.Document.Blocks.Add(new Paragraph(new Run($"Ошибка при чтении файла: {ex.Message}")));
                }
                finally
                {
                    _reader.Close();
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
                // Чтение исходного кода из текстбокса
                TextRange sourceTextRange = new TextRange(SourceTextBox.Document.ContentStart, SourceTextBox.Document.ContentEnd);
                string sourceCode = sourceTextRange.Text;

                // Создаем экземпляр лексического анализатора
                LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer(sourceCode, true); // true - для чтения строки, а не файла

                // Создаем экземпляр синтаксического анализатора
                SyntaxAnalyzer syntaxAnalyzer = new SyntaxAnalyzer(lexicalAnalyzer);

                // Очищаем текстбоксы
                ResultTextBox.Document.Blocks.Clear();
                MessageTextBox.Document.Blocks.Clear();

                // Запускаем компиляцию (синтаксический анализ)
                syntaxAnalyzer.Compile();

                // Выводим все лексемы в ResultTextBox с жирным черным текстом
                List<string> lexemes = lexicalAnalyzer.GetLexemes();
                StringBuilder lexemesOutput = new StringBuilder("Все лексемы в виде списка строк:\n");
                foreach (var lexeme in lexemes)
                {
                    lexemesOutput.AppendLine(lexeme);
                }
                Paragraph resultParagraph = new Paragraph(new Run(lexemesOutput.ToString()))
                {
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black // Черный цвет
                };
                ResultTextBox.Document.Blocks.Add(resultParagraph);

                // Выводим все идентификаторы в таблице имен в ResultTextBox с жирным черным текстом
                StringBuilder identifiersOutput = new StringBuilder("\nВсе идентификаторы в таблице имен:\n");
                foreach (var identifier in lexicalAnalyzer.GetNameTable().GetIdentifiers())
                {
                    identifiersOutput.AppendLine(identifier.ToString());
                }
                Paragraph identifierParagraph = new Paragraph(new Run(identifiersOutput.ToString()))
                {
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black // Черный цвет
                };
                ResultTextBox.Document.Blocks.Add(identifierParagraph);

                // Проверяем наличие ошибок в синтаксическом анализаторе и выводим их красным цветом
                if (syntaxAnalyzer._errors.Count > 0)
                {
                    StringBuilder errorsOutput = new StringBuilder("Ошибки синтаксического анализа:\n");
                    foreach (var error in syntaxAnalyzer._errors)
                    {
                        errorsOutput.AppendLine(error);
                    }
                    Paragraph errorParagraph = new Paragraph(new Run(errorsOutput.ToString()))
                    {
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Red // Красный цвет для ошибок
                    };
                    MessageTextBox.Document.Blocks.Add(errorParagraph);
                }
                else
                {
                    // Выводим сообщение об успешной компиляции в MessageTextBox жирным черным текстом
                    Paragraph successParagraph = new Paragraph(new Run("Компиляция выполнена успешно"))
                    {
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Black // Черный цвет для успешного сообщения
                    };
                    MessageTextBox.Document.Blocks.Add(successParagraph);
                }
            }
            catch (Exception ex)
            {
                // Выводим сообщение об ошибке в MessageTextBox красным цветом
                MessageTextBox.Document.Blocks.Clear();
                Paragraph errorParagraph = new Paragraph(new Run($"Ошибка при компиляции: {ex.Message}"))
                {
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    FontWeight = FontWeights.Normal,
                    Foreground = Brushes.Red // Красный цвет для исключений
                };
                MessageTextBox.Document.Blocks.Add(errorParagraph);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Compile_Click(sender, e); // Вызываем метод компиляции при нажатии Enter
            }
        }

    }
}
