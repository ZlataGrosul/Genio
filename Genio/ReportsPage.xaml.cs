using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Windows.Controls.Primitives;

namespace Genio
{
    public partial class ReportsPage : Page
    {
        private List<string> selectedSpecialties = new List<string>();
        private List<int> selectedCourses = new List<int>();
        private DateTime startDate = new DateTime(2024, 9, 1);
        private DateTime endDate = new DateTime(2024, 12, 31);
        private string reportType = "Сводный по олимпиадам";
        private string generatedReportContent = string.Empty;
        private bool isReportGenerated = false;
        private TextBox currentDateTextBox = null;

        public ReportsPage()
        {
            InitializeComponent();
            LoadDefaultSelections();
            UpdateExportButtonsState();
            
            // обработчик выбора даты в календаре
            DatePickerCalendar.SelectedDatesChanged += DatePickerCalendar_SelectedDatesChanged;
            
            // инициализация текстовых полей дат
            UpdateDateTexts();
        }

        // установка параметров по умочлчанию (выделены все)
        private void LoadDefaultSelections()
        {
            Spec1.IsChecked = true;
            Spec2.IsChecked = true;
            Spec3.IsChecked = true;
            Spec4.IsChecked = true;
            Spec5.IsChecked = true;
            Spec6.IsChecked = true;
            Spec7.IsChecked = true;

            Course1.IsChecked = true;
            Course2.IsChecked = true;
            Course3.IsChecked = true;
            Course4.IsChecked = true;
        }

        private void UpdateDateTexts()
        {
            DateFromText.Text = startDate.ToString("dd.MM.yyyy");
            DateToText.Text = endDate.ToString("dd.MM.yyyy");
        }

        // логика доступности кнопок экспорта
        private void UpdateExportButtonsState()
        {
            if (isReportGenerated)
            {
                ExportPdfBtn.IsEnabled = true;
                ExportWordBtn.IsEnabled = true;
                ExportPdfBtn.Style = (Style)FindResource("AccentButtonStyle");
                ExportWordBtn.Style = (Style)FindResource("AccentButtonStyle");
            }
            else
            {
                ExportPdfBtn.IsEnabled = false;
                ExportWordBtn.IsEnabled = false;
                ExportPdfBtn.Style = (Style)FindResource("DisabledButtonStyle");
                ExportWordBtn.Style = (Style)FindResource("DisabledButtonStyle");
            }
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateSelectedSpecialties();
                UpdateSelectedCourses();

                generatedReportContent = GenerateReportFromData();
                DisplayReportInFlowDocument();

                isReportGenerated = true;
                UpdateExportButtonsState();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPdfBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(generatedReportContent))
            {
                MessageBox.Show("Сначала сформируйте отчет!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PDF файлы (*.pdf)|*.pdf|Все файлы (*.*)|*.*",
                    FileName = $"Отчет_олимпиады_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                    DefaultExt = ".pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToPdf(saveDialog.FileName, generatedReportContent);
                    MessageBox.Show($"PDF-отчет успешно сохранен:\n{saveDialog.FileName}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в PDF: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportWordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(generatedReportContent))
            {
                MessageBox.Show("Сначала сформируйте отчет!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word файлы (*.docx)|*.docx|Все файлы (*.*)|*.*",
                    FileName = $"Отчет_олимпиады_{DateTime.Now:yyyyMMdd_HHmm}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToWord(saveDialog.FileName, generatedReportContent);
                    MessageBox.Show($"Word-отчет успешно сохранен:\n{saveDialog.FileName}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Word: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Очистить все параметры отчета?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Spec1.IsChecked = false;
                Spec2.IsChecked = false;
                Spec3.IsChecked = false;
                Spec4.IsChecked = false;
                Spec5.IsChecked = false;
                Spec6.IsChecked = false;
                Spec7.IsChecked = false;

                Course1.IsChecked = false;
                Course2.IsChecked = false;
                Course3.IsChecked = false;
                Course4.IsChecked = false;

                ReportTypeComboBox.SelectedIndex = 0;
                
                // сброс дат на текущие значения
                startDate = DateTime.Now.AddMonths(-3);
                endDate = DateTime.Now;
                UpdateDateTexts();

                isReportGenerated = false;
                UpdateExportButtonsState();

                ReportDocument.Blocks.Clear();
                generatedReportContent = string.Empty;

            }
        }

        private void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                reportType = selectedItem.Content.ToString();
            }
        }

        private void SpecialtyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedSpecialties();
        }

        private void CourseCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedCourses();
        }

        private void DateFromButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = DateFromText;
            ShowDatePicker(startDate);
        }

        private void DateToButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = DateToText;
            ShowDatePicker(endDate);
        }

        private void ShowDatePicker(DateTime initialDate)
        {
            DatePickerCalendar.SelectedDate = initialDate;
            DatePickerCalendar.DisplayDate = initialDate;
            
            // настройка размещения popup
            if (currentDateTextBox == DateFromText)
            {
                DatePickerPopup.PlacementTarget = DateFromButton;
            }
            else
            {
                DatePickerPopup.PlacementTarget = DateToButton;
            }
            
            DatePickerPopup.IsOpen = true;
        }

        private void DatePickerCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePickerCalendar.SelectedDate.HasValue && currentDateTextBox != null)
            {
                var selectedDate = DatePickerCalendar.SelectedDate.Value;

                if (currentDateTextBox.Name == "DateFromText")
                {
                    startDate = selectedDate;
                }
                else if (currentDateTextBox.Name == "DateToText")
                {
                    endDate = selectedDate;
                }

                currentDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                DatePickerPopup.IsOpen = false;
            }
        }

        private string GenerateReportFromData()
        {
            StringBuilder report = new StringBuilder();

            report.AppendLine("=== ОТЧЕТ ПО ОЛИМПИАДАМ ===");
            report.AppendLine($"Тип отчета: {reportType}");
            report.AppendLine($"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}");
            report.AppendLine();

            report.AppendLine("=== ВЫБРАННЫЕ ПАРАМЕТРЫ ===");
            report.AppendLine($"Специальности: {string.Join(", ", selectedSpecialties)}");
            report.AppendLine($"Курсы: {string.Join(", ", selectedCourses)}");
            report.AppendLine();

            report.AppendLine("=== ДАННЫЕ ИЗ БАЗЫ ДАННЫХ ===");
            report.AppendLine("(Здесь будут реальные данные после подключения БД)");
            report.AppendLine();

            report.AppendLine("Пример результатов:");
            report.AppendLine("1. Математика: 42 награды");
            report.AppendLine("2. Английский язык: 38 наград");
            report.AppendLine("3. Информатика: 25 наград");
            report.AppendLine("4. Физика: 18 наград");
            report.AppendLine("5. Биология: 12 наград");
            report.AppendLine();

            report.AppendLine("=== ВЫВОДЫ ===");
            report.AppendLine("• Отчет сформирован на основе выбранных фильтров");
            report.AppendLine("• Данные будут загружаться из базы данных");
            report.AppendLine($"• Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");

            return report.ToString();
        }

        private void DisplayReportInFlowDocument()
        {
            ReportDocument.Blocks.Clear();

            var title = new System.Windows.Documents.Paragraph
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            title.Inlines.Add(new Run("ОТЧЕТ ПО ОЛИМПИАДАМ"));
            ReportDocument.Blocks.Add(title);

            var section1 = new System.Windows.Documents.Paragraph
            {
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 15, 0, 10),
                Foreground = Brushes.DarkBlue
            };
            section1.Inlines.Add(new Run("ПАРАМЕТРЫ ОТЧЕТА"));
            ReportDocument.Blocks.Add(section1);

            var param1 = new System.Windows.Documents.Paragraph
            {
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            };
            param1.Inlines.Add(new Run($"Тип: {reportType}"));
            ReportDocument.Blocks.Add(param1);

            var param2 = new System.Windows.Documents.Paragraph
            {
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            };
            param2.Inlines.Add(new Run($"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}"));
            ReportDocument.Blocks.Add(param2);

            var param3 = new System.Windows.Documents.Paragraph
            {
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            };
            param3.Inlines.Add(new Run($"Специальности: {string.Join(", ", selectedSpecialties)}"));
            ReportDocument.Blocks.Add(param3);

            var param4 = new System.Windows.Documents.Paragraph
            {
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            };
            param4.Inlines.Add(new Run($"Курсы: {string.Join(", ", selectedCourses)}"));
            ReportDocument.Blocks.Add(param4);

            var section2 = new System.Windows.Documents.Paragraph
            {
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 15, 0, 10),
                Foreground = Brushes.DarkBlue
            };
            section2.Inlines.Add(new Run("РЕЗУЛЬТАТЫ"));
            ReportDocument.Blocks.Add(section2);

            var resultText = new System.Windows.Documents.Paragraph
            {
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            };
            resultText.Inlines.Add(new Run("Данные будут загружены из базы данных после ее подключения."));
            ReportDocument.Blocks.Add(resultText);

            AddSampleTable();

            var info = new System.Windows.Documents.Paragraph
            {
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5),
                FontStyle = FontStyles.Italic
            };
            info.Inlines.Add(new Run($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}"));
            ReportDocument.Blocks.Add(info);
        }

        private void AddSampleTable()
        {
            var table = new System.Windows.Documents.Table
            {
                CellSpacing = 0,
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(50) });
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(100) });
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(120) });

            var headerRow = new System.Windows.Documents.TableRow
            {
                Background = Brushes.LightBlue,
                FontWeight = FontWeights.Bold
            };

            headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new Run("№"))));
            headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new Run("Предмет"))));
            headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new Run("Награды"))));
            headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new Run("Статус"))));

            var rowGroup = new System.Windows.Documents.TableRowGroup();
            rowGroup.Rows.Add(headerRow);
            table.RowGroups.Add(rowGroup);

            var data = new[]
            {
                new { Num = "1", Subject = "Математика", Awards = "42", Status = "Лидер" },
                new { Num = "2", Subject = "Английский язык", Awards = "38", Status = "Высокий" },
                new { Num = "3", Subject = "Информатика", Awards = "25", Status = "Рост +15%" },
                new { Num = "4", Subject = "Физика", Awards = "18", Status = "Стабильный" },
                new { Num = "5", Subject = "Биология", Awards = "12", Status = "Развитие" }
            };

            foreach (var item in data)
            {
                var row = new System.Windows.Documents.TableRow();
                row.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new Run(item.Num))));
                row.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new Run(item.Subject))));
                row.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new Run(item.Awards))));
                row.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new Run(item.Status))));

                rowGroup.Rows.Add(row);
            }

            ReportDocument.Blocks.Add(table);
        }

        private void ExportToPdf(string filePath, string content)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                Document document = new Document(PageSize.A4);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);

                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                document.Add(new iTextSharp.text.Paragraph("ОТЧЕТ ПО ОЛИМПИАДАМ", titleFont));
                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}", normalFont));
                document.Add(new iTextSharp.text.Paragraph(" "));

                string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    document.Add(new iTextSharp.text.Paragraph(line, normalFont));
                }

                document.Close();
            }
        }

        private void ExportToWord(string filePath, string content)
        {
            File.WriteAllText(filePath, content, Encoding.UTF8);
        }

        private void UpdateSelectedSpecialties()
        {
            selectedSpecialties.Clear();
            if (Spec1.IsChecked == true) selectedSpecialties.Add("Планово-экономическая");
            if (Spec2.IsChecked == true) selectedSpecialties.Add("ПОИТ");
            if (Spec3.IsChecked == true) selectedSpecialties.Add("Бухгалтерский учет");
            if (Spec4.IsChecked == true) selectedSpecialties.Add("Банковское дело");
            if (Spec5.IsChecked == true) selectedSpecialties.Add("Правоведение");
            if (Spec6.IsChecked == true) selectedSpecialties.Add("Торговая деятельность");
            if (Spec7.IsChecked == true) selectedSpecialties.Add("Операционная деятельность в логистике");
        }

        private void UpdateSelectedCourses()
        {
            selectedCourses.Clear();
            if (Course1.IsChecked == true) selectedCourses.Add(1);
            if (Course2.IsChecked == true) selectedCourses.Add(2);
            if (Course3.IsChecked == true) selectedCourses.Add(3);
            if (Course4.IsChecked == true) selectedCourses.Add(4);
        }
    }
}