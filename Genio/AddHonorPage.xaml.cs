using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Genio
{
    public partial class AddHonorPage : Page
    {
        private bool isEditMode = false;
        private int honorId = 0;
        private int editYear = 0;
        private List<Student> allStudents = new List<Student>();
        private List<Student> selectedStudents = new List<Student>();
        private List<HonorBoard> existingHonors = new List<HonorBoard>();

        public AddHonorPage()
        {
            InitializeComponent();
            Loaded += AddHonorPage_Loaded;
        }

        public AddHonorPage(int honorIdOrYear) : this()
        {
            if (honorIdOrYear > 2000)
            {
                this.editYear = honorIdOrYear;
                this.isEditMode = true;
            }
            else
            {
                this.honorId = honorIdOrYear;
                this.isEditMode = true;
            }
        }

        private void AddHonorPage_Loaded(object sender, RoutedEventArgs e)
        {
            PlacementDateTextBox.Text = DateTime.Now.ToString("dd.MM.yyyy");
            LoadDataFromDatabase();
            if (isEditMode)
            {
                DeleteButton.Visibility = Visibility.Visible;
                if (editYear > 0)
                {
                    LoadHonorDataByYear(editYear);
                }
                else
                {
                    LoadHonorData();
                }
            }
            SetupEventHandlers();
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allStudents = context.Students_GetAll();
                    LoadStudentsList();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadHonorData()
        {
            if (honorId <= 0) return;
            try
            {
                using (var context = new GenioAppEntities())
                {
                    var honor = context.HonorBoard_GetById(honorId);
                    if (honor != null)
                    {
                        PlacementDateTextBox.Text = honor.placement_date.ToString("dd.MM.yyyy");
                        existingHonors = context.HonorBoard_GetByDate(honor.placement_date);
                        foreach (var existingHonor in existingHonors)
                        {
                            var student = allStudents.FirstOrDefault(s => s.student_id == existingHonor.student_id);
                            if (student != null && !selectedStudents.Any(s => s.student_id == student.student_id))
                            {
                                selectedStudents.Add(student);
                            }
                        }
                        foreach (var student in selectedStudents)
                        {
                            UpdateStudentCard(student);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadHonorDataByYear(int year)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    var allHonors = context.HonorBoard_GetAll();
                    existingHonors = allHonors.Where(h => h.placement_date.Year == year).ToList();
                    if (existingHonors.Any())
                    {
                        PlacementDateTextBox.Text = existingHonors.First().placement_date.ToString("dd.MM.yyyy");
                        foreach (var existingHonor in existingHonors)
                        {
                            var student = allStudents.FirstOrDefault(s => s.student_id == existingHonor.student_id);
                            if (student != null && !selectedStudents.Any(s => s.student_id == student.student_id))
                            {
                                selectedStudents.Add(student);
                            }
                        }
                        foreach (var student in selectedStudents)
                        {
                            UpdateStudentCard(student);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadStudentsList()
        {
            StudentsList.Children.Clear();
            if (allStudents.Count == 0)
            {
                ShowNoResultsMessage("Список студентов пуст");
                return;
            }
            foreach (var student in allStudents)
            {
                CreateStudentCard(student);
            }
        }

        private void CreateStudentCard(Student student)
        {
            var border = new Border();
            bool isSelected = selectedStudents.Any(s => s.student_id == student.student_id);
            if (isSelected)
            {
                border.Style = (Style)FindResource("SelectedStudentItemStyle");
            }
            else
            {
                border.Style = (Style)FindResource("UnselectedStudentItemStyle");
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = $"{student.last_name} {student.first_name} {student.middle_name} • {student.group_name} • {GetCourseName(student.course_number)} курс",
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0),
                TextWrapping = TextWrapping.Wrap
            };

            var button = new Button
            {
                Width = 27,
                Height = 27,
                Tag = student,
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand
            };

            var image = new Image
            {
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            if (isSelected)
            {
                // ✅ ИСПОЛЬЗУЕМ НОВЫЙ СТИЛЬ ДЛЯ КНОПКИ УДАЛЕНИЯ
                button.Style = (Style)FindResource("DeleteListItemButtonStyle");
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png"));
                button.ToolTip = "Удалить из выбранных";
            }
            else
            {
                button.Style = (Style)FindResource("StudentSelectionButtonStyle");
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/addIcon.png"));
                button.ToolTip = "Добавить в выбранные";
            }

            button.Content = image;
            button.Click += StudentSelectionButton_Click;

            Grid.SetColumn(textBlock, 0);
            Grid.SetColumn(button, 1);
            grid.Children.Add(textBlock);
            grid.Children.Add(button);
            border.Child = grid;
            border.Tag = student;
            StudentsList.Children.Add(border);
        }

        private void StudentSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Student student)
            {
                var existingStudent = selectedStudents.FirstOrDefault(s => s.student_id == student.student_id);
                if (existingStudent != null)
                {
                    selectedStudents.Remove(existingStudent);
                }
                else
                {
                    selectedStudents.Add(student);
                }
                UpdateStudentCard(student);
            }
        }

        private void UpdateStudentCard(Student student)
        {
            foreach (var child in StudentsList.Children)
            {
                if (child is Border border && border.Tag is Student stud && stud.student_id == student.student_id)
                {
                    bool isSelected = selectedStudents.Any(s => s.student_id == student.student_id);
                    if (isSelected)
                    {
                        border.Style = (Style)FindResource("SelectedStudentItemStyle");
                    }
                    else
                    {
                        border.Style = (Style)FindResource("UnselectedStudentItemStyle");
                    }

                    if (border.Child is Grid grid)
                    {
                        var button = grid.Children.OfType<Button>().FirstOrDefault();
                        if (button != null)
                        {
                            var image = button.Content as Image;
                            if (image != null)
                            {
                                if (isSelected)
                                {
                                    button.Style = (Style)FindResource("DeleteListItemButtonStyle");
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/iconRemove.png"));
                                    button.ToolTip = "Удалить из выбранных";
                                }
                                else
                                {
                                    button.Style = (Style)FindResource("StudentSelectionButtonStyle");
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/addIcon.png"));
                                    button.ToolTip = "Добавить в выбранные";
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void SetupEventHandlers()
        {
            PlacementDateButton.Click += PlacementDateButton_Click;
            DatePickerCalendar.SelectedDatesChanged += DatePickerCalendar_SelectedDatesChanged;
            StudentSearchTextBox.GotFocus += StudentSearchTextBox_GotFocus;
            StudentSearchTextBox.LostFocus += StudentSearchTextBox_LostFocus;
            StudentSearchTextBox.TextChanged += StudentSearchTextBox_TextChanged;
            StudentSearchTextBox.KeyDown += StudentSearchTextBox_KeyDown;
            BackButton.Click += BackButton_Click;
            SaveButton.Click += SaveButton_Click;
            DeleteButton.Click += DeleteButton_Click;
        }

        private void PlacementDateButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDatePickerForTextBox(PlacementDateTextBox, PlacementDateButton);
        }

        private void ShowDatePickerForTextBox(TextBox textBox, Button button)
        {
            DateTime initialDate;
            if (!DateTime.TryParse(textBox.Text, out initialDate))
                initialDate = DateTime.Now;

            DatePickerCalendar.SelectedDate = initialDate;
            DatePickerCalendar.DisplayDate = initialDate;
            DatePickerPopup.PlacementTarget = button;
            DatePickerPopup.IsOpen = true;
        }

        private void DatePickerCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePickerCalendar.SelectedDate.HasValue && DatePickerPopup.PlacementTarget is Button button)
            {
                var selectedDate = DatePickerCalendar.SelectedDate.Value;
                if (button == PlacementDateButton)
                {
                    PlacementDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                }
                DatePickerPopup.IsOpen = false;
            }
        }

        private void StudentSearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (StudentSearchTextBox.Text == "Поиск...")
            {
                StudentSearchTextBox.Text = "";
                StudentSearchTextBox.Foreground = Brushes.White;
            }
        }

        private void StudentSearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StudentSearchTextBox.Text))
            {
                StudentSearchTextBox.Text = "Поиск...";
                StudentSearchTextBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
            }
        }

        private void StudentSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StudentSearchTextBox.Text != "Поиск..." && !string.IsNullOrWhiteSpace(StudentSearchTextBox.Text))
            {
                FilterStudents(StudentSearchTextBox.Text);
            }
            else if (string.IsNullOrWhiteSpace(StudentSearchTextBox.Text) ||
                     StudentSearchTextBox.Text == "Поиск...")
            {
                LoadStudentsList();
            }
        }

        private void StudentSearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (StudentSearchTextBox.Text != "Поиск..." && !string.IsNullOrWhiteSpace(StudentSearchTextBox.Text))
                {
                    FilterStudents(StudentSearchTextBox.Text);
                }
                Keyboard.ClearFocus();
            }
        }

        private void FilterStudents(string searchText)
        {
            StudentsList.Children.Clear();
            string searchLower = searchText.ToLower();
            var filteredStudents = allStudents.Where(s =>
                s.last_name.ToLower().Contains(searchLower) ||
                s.first_name.ToLower().Contains(searchLower) ||
                s.middle_name.ToLower().Contains(searchLower) ||
                $"{s.last_name} {s.first_name} {s.middle_name}".ToLower().Contains(searchLower) ||
                s.group_name.ToLower().Contains(searchLower) ||
                s.course_number.ToString().Contains(searchText) ||
                GetCourseName(s.course_number).ToLower().Contains(searchLower) ||
                (!string.IsNullOrEmpty(s.phone) && s.phone.Contains(searchText)) ||
                (!string.IsNullOrEmpty(s.home_phone) && s.home_phone.Contains(searchText))
            ).ToList();

            var sortedStudents = filteredStudents
                .OrderBy(s => s.last_name)
                .ThenBy(s => s.first_name)
                .ThenBy(s => s.middle_name)
                .ToList();

            foreach (var student in sortedStudents)
            {
                CreateStudentCard(student);
            }

            if (sortedStudents.Count == 0)
            {
                ShowNoResultsMessage(searchText);
            }
        }

        private string GetCourseName(int courseNumber)
        {
            switch (courseNumber)
            {
                case 1: return "первый";
                case 2: return "второй";
                case 3: return "третий";
                case 4: return "четвертый";
                default: return "";
            }
        }

        private void ShowNoResultsMessage(string searchText)
        {
            var messageBorder = new Border
            {
                Style = (Style)FindResource("UnselectedStudentItemStyle"),
                Margin = new Thickness(0, 10, 0, 0),
                Height = 50
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var textBlock = new TextBlock
            {
                Text = string.IsNullOrEmpty(searchText) ?
                    "Список студентов пуст" :
                    $"По запросу \"{searchText}\" ничего не найдено",
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10),
                TextAlignment = TextAlignment.Center,
                FontStyle = FontStyles.Italic
            };

            Grid.SetColumn(textBlock, 0);
            grid.Children.Add(textBlock);
            messageBorder.Child = grid;
            StudentsList.Children.Add(messageBorder);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LoadPage("HonorBoard");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlacementDateTextBox.Text))
            {
                CustomMessageBox.Show("Выберите дату занесения", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            if (selectedStudents.Count == 0)
            {
                CustomMessageBox.Show("Выберите хотя бы одного студента", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var context = new GenioAppEntities())
                {
                    if (isEditMode)
                    {
                        if (DateTime.TryParse(PlacementDateTextBox.Text, out DateTime placementDate))
                        {
                            if (editYear > 0)
                            {
                                var honorsToDelete = context.HonorBoard_GetAll()
                                    .Where(h => h.placement_date.Year == editYear)
                                    .Select(h => h.honor_id)
                                    .ToList();
                                foreach (var id in honorsToDelete)
                                {
                                    context.HonorBoard_Delete(id);
                                }
                            }
                            else
                            {
                                context.HonorBoard_DeleteByDate(placementDate);
                            }

                            foreach (var student in selectedStudents)
                            {
                                context.HonorBoard_Insert(student.student_id, placementDate);
                            }

                            CustomMessageBox.Show($"Обновлено {selectedStudents.Count} записей на доске почета", "Обновление",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        if (DateTime.TryParse(PlacementDateTextBox.Text, out DateTime placementDate))
                        {
                            foreach (var student in selectedStudents)
                            {
                                context.HonorBoard_Insert(student.student_id, placementDate);
                            }

                            CustomMessageBox.Show($"Добавлено {selectedStudents.Count} записей на доску почета", "Добавление",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                        }
                    }

                    BackButton_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode) return;

            var result = CustomMessageBox.Show("Вы уверены, что хотите удалить записи доски почета?",
                "Подтверждение удаления",
                CustomMessageBoxButton.YesNo, CustomMessageBoxIcon.Warning);

            if (result == CustomMessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenioAppEntities())
                    {
                        if (editYear > 0)
                        {
                            var honorsToDelete = context.HonorBoard_GetAll()
                                .Where(h => h.placement_date.Year == editYear)
                                .ToList();
                            foreach (var honor in honorsToDelete)
                            {
                                context.HonorBoard_Delete(honor.honor_id);
                            }
                            CustomMessageBox.Show($"Удалено {honorsToDelete.Count} записей с доски почета за {editYear} год", "Удаление",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                        }
                        else if (honorId > 0)
                        {
                            var honor = context.HonorBoard_GetById(honorId);
                            if (honor != null)
                            {
                                context.HonorBoard_DeleteByDate(honor.placement_date);
                                CustomMessageBox.Show("Записи с доски почета удалены", "Удаление",
                                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                            }
                        }

                        BackButton_Click(sender, e);
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                }
            }
        }
    }
}