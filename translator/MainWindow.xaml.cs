using Microsoft.Win32;
using System;
using System.Diagnostics; // Добавлено для работы с процессами
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Lexical_Analyzer_Libary.Classes;
using System.Linq;

namespace translator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process process; // Поле для управления процессом DOSBox

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // Код для открытия файла
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
            // Код для сохранения файла
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

                    // Получаем сгенерированный ассемблерный код из CodeGenerator
                    string[] generatedCode = CodeGenerator.GetGeneratedCode();

                    // Формируем строку для вывода
                    StringBuilder codeOutput = new StringBuilder("Сгенерированный ассемблерный код:\n");
                    foreach (var line in generatedCode)
                    {
                        codeOutput.AppendLine(line);
                    }

                    // Выводим сгенерированный код в ResultTextBox
                    Paragraph codeParagraph = new Paragraph(new Run(codeOutput.ToString()))
                    {
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 12,
                        FontWeight = FontWeights.Normal,
                        Foreground = Brushes.Black // Черный цвет для кода
                    };
                    ResultTextBox.Document.Blocks.Add(codeParagraph);

                    // Сохраняем сгенерированный код в файл Code.asm
                    File.WriteAllLines("TASM\\Code.asm", generatedCode);
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

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, есть ли ошибки компиляции
            if (MessageTextBox.Document.Blocks.Any(block => ((Run)((Paragraph)block).Inlines.FirstInline).Foreground == Brushes.Red))
            {
                MessageBox.Show("Обнаружены ошибки компиляции. Исполнение программы невозможно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Получаем текущую директорию и путь к папке TASM
                string tasmPath = Environment.CurrentDirectory + "\\TASM";

                // Проверяем существование необходимых файлов
                if (!File.Exists(tasmPath + "\\DOSBox.exe"))
                {
                    MessageBox.Show("Не найден DOSBox.exe в папке TASM.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!File.Exists(tasmPath + "\\Run.bat"))
                {
                    MessageBox.Show("Не найден Run.bat в папке TASM.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем процесс для запуска DOSBox с нашим пакетным файлом
                process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = tasmPath + "\\DOSBox.exe",
                    Arguments = $"\"{tasmPath}\\Run.bat\" -noconsole",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tasmPath
                };
                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске программы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
