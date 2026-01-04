using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Genio
{
    public partial class AddEditOlimpPage : Page
    {
        private bool isEditMode = false;
        private int eventId = 0;
        private List<StudentItem> students = new List<StudentItem>();
        private TextBox currentDateTextBox = null;

        public class StudentItem
        {
            public string Name { get; set; }
            public string Group { get; set; }
            public bool IsSelected { get; set; }
            public Border StudentBorder { get; set; }
            public Button SelectionButton { get; set; }
            public Image ButtonImage { get; set; }
        }

        public AddEditOlimpPage()
        {
            InitializeComponent();
            Loaded += AddEditOlimpPage_Loaded;
        }

        public AddEditOlimpPage(int eventId) : this()
        {
            this.eventId = eventId;
            this.isEditMode = true;
        }

        private void AddEditOlimpPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTestStudents();

            // устанавливаем режим страницы
            if (isEditMode)
            {
                // режим редактирования
                DeleteButton.IsEnabled = true;
                // Загружаем данные мероприятия из БД по eventId
                LoadEventData(eventId);
            }
            else
            {
                // Режим добавления
                DeleteButton.IsEnabled = false;
                // Устанавливаем значения по умолчанию
                SetDefaultValues();
            }
        }

        private void InitializeTestStudents()
        {
            // Тестовые данные учащихся
            students = new List<StudentItem>
            {
                new StudentItem { Name = "Грекул Злата Анатольевна", Group = "1-291", IsSelected = true },
                new StudentItem { Name = "Мотелек Маргарита Михайловна", Group = "1-292", IsSelected = true },
                new StudentItem { Name = "Садковская Валерия Викторовна", Group = "1-291", IsSelected = false },
                new StudentItem { Name = "Пижонт Маргарита Сергеевна", Group = "1-293", IsSelected = false },
                new StudentItem { Name = "Иванов Алексей Иванович", Group = "1-291", IsSelected = false },
                new StudentItem { Name = "Паку Евгений Андреевич", Group = "1-492", IsSelected = false },
                new StudentItem { Name = "Марина Ирина Викторовна", Group = "1-492", IsSelected = false }
            };

            // Очищаем список
            StudentsList.Children.Clear();

            // Создаем элементы списка
            foreach (var student in students)
            {
                CreateStudentItem(student);
            }
        }

        private void CreateStudentItem(StudentItem student)
        {
            // Создаем Border для элемента
            var border = new Border();
            student.StudentBorder = border;

            // Устанавливаем стиль в зависимости от выбора
            if (student.IsSelected)
            {
                border.Style = (Style)FindResource("SelectedStudentItemStyle");
            }
            else
            {
                border.Style = (Style)FindResource("UnselectedStudentItemStyle");
            }

            // Создаем Grid для содержимого
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Текст с ФИО и группой
            var textBlock = new TextBlock
            {
                Text = $"{student.Name} • {student.Group}",
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            // Кнопка выбора
            var button = new Button
            {
                Style = (Style)FindResource("StudentSelectionButtonStyle"),
                Margin = new Thickness(0, 0, 5, 0),
                Tag = student
            };

            student.SelectionButton = button;

            // Иконка кнопки
            var image = new Image
            {
                Stretch = Stretch.Uniform,
                // Устанавливаем размеры в зависимости от состояния
                Width = student.IsSelected ? 13 : 18, // iconRemove меньше, addIcon больше
                Height = student.IsSelected ? 13 : 18
            };

            student.ButtonImage = image;

            // Устанавливаем иконку в зависимости от состояния
            if (student.IsSelected)
            {
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png"));
            }
            else
            {
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/addIcon.png"));
            }

            button.Content = image;

            // Обработчик клика
            button.Click += StudentSelectionButton_Click;

            // Добавляем элементы в Grid
            Grid.SetColumn(textBlock, 0);
            Grid.SetColumn(button, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(button);

            border.Child = grid;

            // Добавляем в список
            StudentsList.Children.Add(border);
        }

        private void StudentSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is StudentItem student)
            {
                // Меняем состояние выбора
                student.IsSelected = !student.IsSelected;

                // Обновляем стиль Border
                if (student.IsSelected)
                {
                    student.StudentBorder.Style = (Style)FindResource("SelectedStudentItemStyle");
                    student.ButtonImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("pack://application:,,,/Images/iconRemove.png"));
                    // Устанавливаем меньший размер для iconRemove
                    student.ButtonImage.Width = 13;
                    student.ButtonImage.Height = 13;
                }
                else
                {
                    student.StudentBorder.Style = (Style)FindResource("UnselectedStudentItemStyle");
                    student.ButtonImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("pack://application:,,,/Images/addIcon.png"));
                    // Устанавливаем больший размер для addIcon
                    student.ButtonImage.Width = 18;
                    student.ButtonImage.Height = 18;
                }
            }
        }

        private void LoadEventData(int eventId)
        {
            // TODO: Загрузка данных из БД по eventId
            EventDateTextBox.Text = "01.03.2024";
            EventNameTextBox.Text = "Зимний Чемпионат по управлению бизнесом среди молодёжи \"Виртуальный бизнес-комп\"";
            EventLevelComboBox.SelectedIndex = 0;
            EventTypeComboBox.SelectedIndex = 0;
            EventResultTextBox.Text = "2 место";
            EventNominationsTextBox.Text = "\"Моделирование экономики\" группа \"Лидер\"";
            EventLocationTextBox.Text = "Центр поддержки и развития юношеского предпринимательства";
        }

        private void SetDefaultValues()
        {
            EventDateTextBox.Text = DateTime.Now.ToString("dd.MM.yyyy");
            EventNameTextBox.Text = "";
            EventLevelComboBox.SelectedIndex = 0;
            EventTypeComboBox.SelectedIndex = 0;
            EventResultTextBox.Text = "";
            EventNominationsTextBox.Text = "";
            EventLocationTextBox.Text = "";
        }

        // DatePicker логика
        private void EventDateButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = EventDateTextBox;
            ShowDatePicker(DateTime.Now);
        }

        private void ShowDatePicker(DateTime initialDate)
        {
            DatePickerCalendar.SelectedDate = initialDate;
            DatePickerCalendar.DisplayDate = initialDate;
            DatePickerPopup.PlacementTarget = EventDateButton;
            DatePickerPopup.IsOpen = true;
        }

        private void DatePickerCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePickerCalendar.SelectedDate.HasValue && currentDateTextBox != null)
            {
                var selectedDate = DatePickerCalendar.SelectedDate.Value;
                currentDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                DatePickerPopup.IsOpen = false;
            }
        }

        // Поиск учащихся
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
            else
            {
                // TODO: Фильтрация списка учащихся по поисковому запросу
                FilterStudents(StudentSearchTextBox.Text);
            }
        }

        private void FilterStudents(string searchText)
        {
            // TODO: Реализовать фильтрацию списка учащихся
            // Показываем всех учащихся при пустом поиске
            StudentsList.Children.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (var student in students)
                {
                    CreateStudentItem(student);
                }
            }
            else
            {
                foreach (var student in students)
                {
                    if (student.Name.ToLower().Contains(searchText.ToLower()) ||
                        student.Group.ToLower().Contains(searchText.ToLower()))
                    {
                        CreateStudentItem(student);
                    }
                }
            }
        }

        // Кнопки управления
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на страницу мероприятий
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LoadPage("Olympiads");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(EventNameTextBox.Text))
            {
                MessageBox.Show("Введите название мероприятия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EventDateTextBox.Text))
            {
                MessageBox.Show("Выберите дату мероприятия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Сохранение данных в БД
            if (isEditMode)
            {
                MessageBox.Show("Изменения сохранены", "Сохранение",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Новое мероприятие добавлено", "Добавление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Возврат на страницу мероприятий
            BackButton_Click(sender, e);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (isEditMode && eventId > 0)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить это мероприятие?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Удаление из БД
                    MessageBox.Show("Мероприятие удалено", "Удаление",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Возврат на страницу мероприятий
                    BackButton_Click(sender, e);
                }
            }
        }
    }
}