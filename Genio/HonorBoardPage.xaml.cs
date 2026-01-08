using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Genio
{
    public partial class HonorBoardPage : Page
    {
        private bool filtersVisible = false;
        private List<int> selectedSpecialties = new List<int>();
        private List<int> selectedCourses = new List<int>();
        private int selectedYear = 0;
        private string currentSearchText = "";
        private Dictionary<CheckBox, int> specialtyCheckBoxes = new Dictionary<CheckBox, int>();
        private List<HonorDisplayItem> allHonorItems = new List<HonorDisplayItem>();
        private ICollectionView honorItemsView;

        // Простой класс для отображения данных
        public class HonorDisplayItem
        {
            public string FullName { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public int Course { get; set; }
            public string Group { get; set; }
            public DateTime AdmissionDate { get; set; }
            public DateTime GraduationDate { get; set; }
            public DateTime PlacementDate { get; set; }
            public int StudentId { get; set; }
            public int HonorId { get; set; }
        }

        public HonorBoardPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadYearComboBox();
            LoadSpecialtiesFromDatabase();
            LoadDefaultFilters();
            LoadHonorBoardData();
        }

        private void LoadYearComboBox()
        {
            try
            {
                using (var db = new GenioAppEntities())
                {
                    YearComboBox.Items.Clear();

                    YearComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = "Все годы",
                        Tag = 0
                    });

                    var years = db.HonorBoards
                        .Select(h => h.placement_date.Year)
                        .Distinct()
                        .OrderByDescending(y => y)
                        .ToList();

                    foreach (var year in years)
                    {
                        YearComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = year.ToString(),
                            Tag = year
                        });
                    }

                    YearComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка годов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSpecialtiesFromDatabase()
        {
            try
            {
                using (var db = new GenioAppEntities())
                {
                    var specialties = db.Specializations
                        .OrderBy(s => s.spec_name)
                        .ToList();

                    SpecialtiesFilterPanel.Children.Clear();
                    specialtyCheckBoxes.Clear();

                    foreach (var specialty in specialties)
                    {
                        var checkBox = new CheckBox
                        {
                            Content = specialty.spec_name,
                            Margin = new Thickness(0, 0, 0, 6),
                            Style = (Style)FindResource("ReportCheckBoxStyle"),
                            FontSize = 12,
                            Tag = specialty.specialization_id
                        };

                        checkBox.Checked += SpecialtyFilter_Changed;
                        checkBox.Unchecked += SpecialtyFilter_Changed;

                        specialtyCheckBoxes.Add(checkBox, specialty.specialization_id);
                        SpecialtiesFilterPanel.Children.Add(checkBox);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки специальностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDefaultFilters()
        {
            foreach (var checkBox in specialtyCheckBoxes.Keys)
            {
                checkBox.IsChecked = true;
            }

            FilterCourse1.IsChecked = true;
            FilterCourse2.IsChecked = true;
            FilterCourse3.IsChecked = true;
            FilterCourse4.IsChecked = true;

            UpdateSelectedSpecialties();
            UpdateSelectedCourses();
        }

        private void LoadHonorBoardData()
        {
            try
            {
                using (var db = new GenioAppEntities())
                {
                    var query = db.HonorBoards
                        .Include("Student")
                        .Include("Student.Specialization")
                        .AsQueryable();

                    if (selectedYear > 0)
                    {
                        query = query.Where(h => h.placement_date.Year == selectedYear);
                    }

                    if (selectedCourses.Any())
                    {
                        query = query.Where(h => selectedCourses.Contains(h.Student.course_number));
                    }

                    if (selectedSpecialties.Any())
                    {
                        query = query.Where(h => selectedSpecialties.Contains(h.Student.specialization_id));
                    }

                    var honorRecords = query
                        .OrderByDescending(h => h.placement_date)
                        .ThenBy(h => h.Student.last_name)
                        .ThenBy(h => h.Student.first_name)
                        .ThenBy(h => h.Student.middle_name)
                        .ToList();

                    allHonorItems = new List<HonorDisplayItem>();

                    foreach (var record in honorRecords)
                    {
                        var student = record.Student;
                        allHonorItems.Add(new HonorDisplayItem
                        {
                            FullName = $"{student.last_name} {student.first_name} {student.middle_name}",
                            LastName = student.last_name,
                            FirstName = student.first_name,
                            MiddleName = student.middle_name,
                            Course = student.course_number,
                            Group = student.group_name,
                            AdmissionDate = student.admission_date,
                            GraduationDate = student.graduation_date ?? student.admission_date.AddYears(4),
                            PlacementDate = record.placement_date,
                            StudentId = student.student_id,
                            HonorId = record.honor_id
                        });
                    }

                    // Создаем CollectionView для фильтрации
                    honorItemsView = CollectionViewSource.GetDefaultView(allHonorItems);
                    honorItemsView.Filter = HonorItemFilter;

                    HonorBoardGrid.ItemsSource = null;
                    HonorBoardGrid.ItemsSource = honorItemsView;

                    ExportExcelBtn.IsEnabled = allHonorItems.Any();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных доски почета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool HonorItemFilter(object item)
        {
            // Если поле поиска пустое или содержит текст "Поиск...", показываем все записи
            if (string.IsNullOrWhiteSpace(currentSearchText) || currentSearchText == "Поиск...")
                return true;

            var honorItem = item as HonorDisplayItem;
            if (honorItem == null)
                return false;

            string searchLower = currentSearchText.ToLower().Trim();

            // Разделяем поисковый запрос на слова
            var searchWords = searchLower.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Проверяем все комбинации поиска
            foreach (var word in searchWords)
            {
                bool wordMatches =
                    // Поиск по фамилии
                    honorItem.LastName.ToLower().Contains(word) ||
                    // Поиск по имени
                    honorItem.FirstName.ToLower().Contains(word) ||
                    // Поиск по отчеству
                    honorItem.MiddleName.ToLower().Contains(word) ||
                    // Поиск по полному ФИО
                    honorItem.FullName.ToLower().Contains(word) ||
                    // Поиск по группе (дополнительно)
                    honorItem.Group.ToLower().Contains(word);

                if (!wordMatches)
                    return false;
            }

            return true;
        }

        private void UpdateSelectedSpecialties()
        {
            selectedSpecialties.Clear();
            foreach (var kvp in specialtyCheckBoxes)
            {
                if (kvp.Key.IsChecked == true)
                {
                    selectedSpecialties.Add(kvp.Value);
                }
            }
        }

        private void UpdateSelectedCourses()
        {
            selectedCourses.Clear();
            if (FilterCourse1.IsChecked == true) selectedCourses.Add(1);
            if (FilterCourse2.IsChecked == true) selectedCourses.Add(2);
            if (FilterCourse3.IsChecked == true) selectedCourses.Add(3);
            if (FilterCourse4.IsChecked == true) selectedCourses.Add(4);
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск...";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Обновляем текущий текст поиска
            currentSearchText = SearchTextBox.Text;

            // Если поле поиска пустое или содержит "Поиск...", сбрасываем фильтр
            if (string.IsNullOrWhiteSpace(currentSearchText) || currentSearchText == "Поиск...")
            {
                currentSearchText = "";
            }

            // Применяем фильтр
            ApplySearchFilter();
        }

        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ApplySearchFilter();
                // Убираем фокус с TextBox после нажатия Enter
                System.Windows.Input.Keyboard.ClearFocus();
            }
        }

        private void ApplySearchFilter()
        {
            if (honorItemsView != null)
            {
                honorItemsView.Refresh();
            }
        }

        private void FiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            filtersVisible = !filtersVisible;

            if (filtersVisible)
            {
                FiltersPanel.Visibility = Visibility.Visible;
                ActionButtonsPanel.Visibility = Visibility.Collapsed;
                FiltersBtn.Style = (Style)FindResource("FiltersButtonActiveStyle");
            }
            else
            {
                FiltersPanel.Visibility = Visibility.Collapsed;
                ActionButtonsPanel.Visibility = Visibility.Visible;
                FiltersBtn.Style = (Style)FindResource("FiltersButtonStyle");
            }
        }

        private void ExportExcelBtn_Click(object sender, RoutedEventArgs e)
        {
            var itemsToExport = honorItemsView?.Cast<HonorDisplayItem>().ToList() ?? allHonorItems;

            if (itemsToExport.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Excel файлы (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
                    FileName = $"Доска_почета_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCSV(saveDialog.FileName, itemsToExport);
                    MessageBox.Show($"Данные успешно экспортированы!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCSV(string filePath, List<HonorDisplayItem> items)
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    // Заголовки
                    writer.WriteLine("ФИО;Курс;Группа;Дата поступления;Дата окончания;Дата занесения на ДП");

                    foreach (var item in items)
                    {
                        writer.WriteLine($"\"{item.FullName}\";{item.Course};\"{item.Group}\";" +
                                       $"{item.AdmissionDate:dd.MM.yyyy};{item.GraduationDate:dd.MM.yyyy};" +
                                       $"{item.PlacementDate:dd.MM.yyyy}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения CSV: {ex.Message}");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу добавления записи
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var page = new AddHonorPage();
                mainWindow.MainFrame.Navigate(page);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (HonorBoardGrid.SelectedItem != null)
            {
                var selectedItem = HonorBoardGrid.SelectedItem as HonorDisplayItem;
                if (selectedItem != null)
                {
                    // Переход на страницу редактирования записи
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    if (mainWindow != null)
                    {
                        var page = new AddHonorPage(selectedItem.HonorId);
                        mainWindow.MainFrame.Navigate(page);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите запись для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            // Сброс фильтров специальностей
            foreach (var checkBox in specialtyCheckBoxes.Keys)
            {
                checkBox.IsChecked = true;
            }

            // Сброс фильтров курсов
            FilterCourse1.IsChecked = true;
            FilterCourse2.IsChecked = true;
            FilterCourse3.IsChecked = true;
            FilterCourse4.IsChecked = true;

            // Сброс года
            YearComboBox.SelectedIndex = 0;

            // Сброс поиска
            SearchTextBox.Text = "Поиск...";
            SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            currentSearchText = "";

            // Обновление фильтров
            UpdateSelectedSpecialties();
            UpdateSelectedCourses();
            selectedYear = 0;

            // Перезагрузка данных
            LoadHonorBoardData();
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (YearComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedYear = selectedItem.Tag != null ? (int)selectedItem.Tag : 0;
                LoadHonorBoardData();
            }
        }

        private void SpecialtyFilter_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedSpecialties();
            LoadHonorBoardData();
        }

        private void CourseFilter_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedCourses();
            LoadHonorBoardData();
        }

        private void SearchTextBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.White;
                SearchTextBox.Focus();
                e.Handled = true;
            }
        }

        private void HonorBoardGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (HonorBoardGrid.SelectedItem != null)
            {
                EditButton_Click(sender, e);
            }
        }
    }
}