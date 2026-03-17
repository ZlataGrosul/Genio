using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace Genio
{
    public partial class AnalyticsPage : Page
    {
        private GenioAppEntities db = new GenioAppEntities();
        private DispatcherTimer timer;

        // Цвета для диаграмм
        private Brush[] chartColors;

        public AnalyticsPage()
        {
            InitializeComponent();

            // Инициализируем цвета после загрузки ресурсов
            InitializeColors();

            // Устанавливаем текущую дату
            CurrentDateText.Text = DateTime.Now.ToString("dd.MM.yyyy");

            // Настраиваем таймер для обновления данных
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(5); // Обновление каждые 5 минут
            timer.Tick += Timer_Tick;
        }

        private void InitializeColors()
        {
            // Инициализируем цвета из ресурсов
            chartColors = new Brush[]
            {
                TryFindResource("StudentCardBrush1") as Brush ?? Brushes.Purple,
                TryFindResource("StudentCardBrush2") as Brush ?? Brushes.MediumPurple,
                TryFindResource("StudentCardBrush3") as Brush ?? Brushes.DarkViolet,
                TryFindResource("StudentCardBrush4") as Brush ?? Brushes.Indigo,
                TryFindResource("StudentCardBrush5") as Brush ?? Brushes.DarkMagenta,
                Brushes.Violet,
                Brushes.Plum,
                Brushes.Orchid,
                Brushes.MediumOrchid,
                Brushes.DarkOrchid,
                Brushes.BlueViolet,
                Brushes.MediumPurple
            };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAnalyticsData();
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadAnalyticsData();
        }

        private async void LoadAnalyticsData()
        {
            try
            {
                // Загрузка данных асинхронно
                await LoadOlimpAnalyticsAsync();
                await LoadRatingTableAsync();
                await LoadSpecializationStatsAsync();
                await LoadActiveGroupAsync();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки аналитики: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private async Task LoadOlimpAnalyticsAsync()
        {
            try
            {
                var currentDate = DateTime.Now;
                var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // Подсчет ВСЕХ мероприятий за текущий месяц (все типы событий)
                var olimpsThisMonth = await db.Olimps
                    .Where(o => o.olimp_date >= firstDayOfMonth && o.olimp_date <= lastDayOfMonth)
                    .CountAsync();

                OlimpCountText.Text = olimpsThisMonth.ToString();

                // Построение графика за последние 4 месяца
                await BuildBarChartAsync();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки статистики олимпиад: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                OlimpCountText.Text = "0";
            }
        }

        private async Task BuildBarChartAsync()
        {
            try
            {
                var currentDate = DateTime.Now;
                var monthsData = new List<MonthData>();

                // Собираем данные за последние 4 месяца
                for (int i = 3; i >= 0; i--)
                {
                    var monthDate = currentDate.AddMonths(-i);
                    var firstDayOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    var count = await db.Olimps
                        .Where(o => o.olimp_date >= firstDayOfMonth && o.olimp_date <= lastDayOfMonth)
                        .CountAsync();

                    monthsData.Add(new MonthData
                    {
                        MonthName = monthDate.ToString("MMM").ToUpper(),
                        MonthNumber = monthDate.Month,
                        Year = monthDate.Year,
                        OlimpCount = count
                    });
                }

                // Очищаем старые элементы
                BarChartGrid.Children.Clear();
                MonthLegendPanel.Children.Clear();

                // Находим максимальное значение для масштабирования
                int maxCount = monthsData.Max(m => m.OlimpCount);
                if (maxCount == 0) maxCount = 1; // Избегаем деления на ноль

                // Создаем столбцы диаграммы
                double columnWidth = 24;
                double spacing = 30;
                double startX = 20;
                double maxHeight = 100; // Максимальная высота столбца

                for (int i = 0; i < monthsData.Count; i++)
                {
                    var month = monthsData[i];

                    // Создаем контейнер для столбца
                    var columnContainer = new StackPanel
                    {
                        Width = columnWidth,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(startX + i * (columnWidth + spacing), 0, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    // Высота столбца (нормализованная)
                    double normalizedHeight = (double)month.OlimpCount / maxCount * maxHeight;
                    if (normalizedHeight < 5 && month.OlimpCount > 0) normalizedHeight = 5; // Минимальная высота

                    // Сам столбец
                    var column = new Border
                    {
                        Height = normalizedHeight,
                        Style = (Style)TryFindResource("ChartColumnStyle"),
                        Background = chartColors[i % chartColors.Length],
                        ToolTip = $"{month.MonthName}: {month.OlimpCount} мероприятий"
                    };

                    // Если стиль не найден, задаем параметры напрямую
                    if (column.Style == null)
                    {
                        column.CornerRadius = new CornerRadius(4, 4, 0, 0);
                        column.Cursor = Cursors.Hand;
                        column.MouseEnter += (s, e) => column.Background = new SolidColorBrush(Color.FromRgb(80, 69, 213));
                        column.MouseLeave += (s, e) => column.Background = chartColors[i % chartColors.Length];
                    }

                    // Текст над столбцом
                    var countText = new TextBlock
                    {
                        Text = month.OlimpCount.ToString(),
                        Foreground = Brushes.White,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 5)
                    };

                    columnContainer.Children.Add(countText);
                    columnContainer.Children.Add(column);

                    BarChartGrid.Children.Add(columnContainer);

                    // Добавляем месяц в легенду
                    var legendItem = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(10, 0, 10, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var colorBox = new Border
                    {
                        Width = 10,
                        Height = 10,
                        Background = chartColors[i % chartColors.Length],
                        Margin = new Thickness(0, 0, 5, 0),
                        CornerRadius = new CornerRadius(2)
                    };

                    var monthText = new TextBlock
                    {
                        Text = month.MonthName,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140)),
                        FontSize = 11
                    };

                    legendItem.Children.Add(colorBox);
                    legendItem.Children.Add(monthText);

                    MonthLegendPanel.Children.Add(legendItem);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка построения графика: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private async Task LoadRatingTableAsync()
        {
            try
            {
                // Получаем студентов с количеством участий в олимпиадах
                var students = await db.Students
                    .Include(s => s.StudentOlimps)
                    .Select(s => new
                    {
                        s.student_id,
                        FullName = s.last_name + " " + s.first_name + (s.middle_name != null ? " " + s.middle_name : ""),
                        OlimpCount = s.StudentOlimps.Count(),
                        s.course_number,
                        s.group_name
                    })
                    .Where(s => s.OlimpCount > 0) // Только те, кто участвовал
                    .OrderByDescending(s => s.OlimpCount)
                    .ThenBy(s => s.FullName)
                    .Take(10) // Топ-10 студентов
                    .ToListAsync();

                // Преобразуем в список для DataGrid
                var ratingList = new List<RatingItem>();
                int position = 1;

                foreach (var student in students)
                {
                    ratingList.Add(new RatingItem
                    {
                        Position = position++,
                        FullName = student.FullName,
                        OlimpCount = student.OlimpCount,
                        Course = student.course_number,
                        Group = student.group_name
                    });
                }

                RatingDataGrid.ItemsSource = ratingList;

                if (!ratingList.Any())
                {
                    RatingDataGrid.ItemsSource = new List<RatingItem>
                    {
                        new RatingItem { Position = 1, FullName = "Нет данных", OlimpCount = 0, Course = 0, Group = "" }
                    };
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки рейтинговой таблицы: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private async Task LoadSpecializationStatsAsync()
        {
            try
            {
                var stats = await db.Students
                    .Include(s => s.Specialization)
                    .Include(s => s.StudentOlimps)
                    .GroupBy(s => s.Specialization.spec_name)
                    .Select(g => new
                    {
                        Specialization = g.Key,
                        StudentCount = g.Count(),
                        OlimpCount = g.Sum(s => s.StudentOlimps.Count())
                    })
                    .Where(s => s.OlimpCount > 0) // Только специальности с участиями
                    .OrderByDescending(s => s.OlimpCount)
                    .ToListAsync();

                // Очищаем предыдущую диаграмму
                PieChartCanvas.Children.Clear();
                SpecializationLegend.Children.Clear();

                if (!stats.Any())
                {
                    // Отображаем сообщение об отсутствии данных
                    var noDataText = new TextBlock
                    {
                        Text = "Нет данных",
                        Foreground = Brushes.White,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Canvas.SetLeft(noDataText, 65);
                    Canvas.SetTop(noDataText, 115);
                    PieChartCanvas.Children.Add(noDataText);
                    return;
                }

                // Подготовка данных для круговой диаграммы
                double total = stats.Sum(s => s.OlimpCount);
                double startAngle = 0;
                double centerX = 125; // Центр для Canvas 250x250
                double centerY = 125;
                double radius = 90; // Увеличенный радиус

                // Создаем сектора диаграммы
                for (int i = 0; i < stats.Count; i++)
                {
                    var stat = stats[i];
                    double percentage = (double)stat.OlimpCount / total;
                    double sweepAngle = percentage * 360;

                    // Создаем сектор
                    var path = CreatePieSlice(centerX, centerY, radius, startAngle, sweepAngle);
                    path.Fill = chartColors[i % chartColors.Length];

                    // Применяем стиль
                    var style = (Style)TryFindResource("PieSliceStyle");
                    if (style != null)
                    {
                        path.Style = style;
                    }
                    else
                    {
                        path.Stroke = TryFindResource("ReportFrameColor") as Brush ?? Brushes.Gray;
                        path.StrokeThickness = 1.5;
                        path.Cursor = Cursors.Hand;
                        path.MouseEnter += (s, e) => path.Opacity = 0.9;
                        path.MouseLeave += (s, e) => path.Opacity = 1.0;
                    }

                    path.Tag = stat;
                    path.ToolTip = $"{stat.Specialization}\nУчастий: {stat.OlimpCount} ({percentage:P1})";

                    // Обработчик клика
                    path.MouseLeftButtonDown += (s, e) =>
                    {
                        CustomMessageBox.Show($"{stat.Specialization}\nУчащихся: {stat.StudentCount}\nУчастий в мероприятиях: {stat.OlimpCount}",
                            "Статистика специальности");
                    };

                    PieChartCanvas.Children.Add(path);

                    // Добавляем в легенду
                    var legendItem = CreateLegendItem(
                        stat.Specialization,
                        stat.OlimpCount,
                        percentage,
                        chartColors[i % chartColors.Length]
                    );

                    SpecializationLegend.Children.Add(legendItem);

                    startAngle += sweepAngle;
                }

                // Добавляем текст в центре диаграммы с улучшенным форматированием
                var centerText = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 100,
                    Height = 40
                };

                var totalText = new TextBlock
                {
                    Text = "Всего",
                    Foreground = Brushes.LightGray,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var countText = new TextBlock
                {
                    Text = total.ToString(),
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                centerText.Children.Add(totalText);
                centerText.Children.Add(countText);

                Canvas.SetLeft(centerText, centerX - 50);
                Canvas.SetTop(centerText, centerY - 20);
                PieChartCanvas.Children.Add(centerText);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки статистики специальностей: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        // ОБНОВЛЕННЫЙ МЕТОД ДЛЯ ЛЕГЕНДЫ
        private StackPanel CreateLegendItem(string specializationName, int count, double percentage, Brush color)
        {
            var legendItem = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 8),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                ToolTip = $"{specializationName}: {count} участ. ({percentage:P1})"
            };

            // Кружок увеличенного размера
            var circle = new Ellipse
            {
                Width = 16, 
                Height = 16,
                Fill = color,
                Margin = new Thickness(0, 0, 12, 0),
                Stroke = Brushes.White,
                StrokeThickness = 1.5,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Текст с увеличенным шрифтом
            var text = new TextBlock
            {
                Text = $"{GetShortSpecializationName(specializationName)}: {count} ({percentage:P1})",
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 300,
                Margin = new Thickness(0, 0, 0, 0)
            };

            legendItem.Children.Add(circle);
            legendItem.Children.Add(text);

            return legendItem;
        }

        private async Task LoadActiveGroupAsync()
        {
            try
            {
                // Находим группу с максимальным количеством студентов
                var activeGroup = await db.Students
                    .GroupBy(s => s.group_name)
                    .Select(g => new
                    {
                        GroupName = g.Key,
                        StudentCount = g.Count()
                    })
                    .OrderByDescending(g => g.StudentCount)
                    .FirstOrDefaultAsync();

                if (activeGroup != null)
                {
                    ActiveGroupText.Text = activeGroup.GroupName;
                }
                else
                {
                    ActiveGroupText.Text = "Нет данных";
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки активной группы: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                ActiveGroupText.Text = "Ошибка";
            }
        }

        private Path CreatePieSlice(double centerX, double centerY, double radius,
                                   double startAngle, double sweepAngle)
        {
            var path = new Path();

            var geometry = new PathGeometry();
            var figure = new PathFigure();
            figure.StartPoint = new Point(centerX, centerY);

            // Добавляем линию к начальной точке дуги
            double startRadians = (startAngle - 90) * Math.PI / 180;
            var startPoint = new Point(
                centerX + radius * Math.Cos(startRadians),
                centerY + radius * Math.Sin(startRadians));
            figure.Segments.Add(new LineSegment(startPoint, true));

            // Добавляем дугу
            double endRadians = (startAngle + sweepAngle - 90) * Math.PI / 180;
            var endPoint = new Point(
                centerX + radius * Math.Cos(endRadians),
                centerY + radius * Math.Sin(endRadians));

            var arcSegment = new ArcSegment(
                endPoint,
                new Size(radius, radius),
                0,
                sweepAngle > 180,
                SweepDirection.Clockwise,
                true);

            figure.Segments.Add(arcSegment);

            figure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));
            geometry.Figures.Add(figure);
            path.Data = geometry;

            return path;
        }

        private string GetShortSpecializationName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "Спец.";

            // Сокращаем длинные названия для лучшего отображения
            if (fullName.Length > 25)
            {
                // Ищем основную часть названия
                if (fullName.Contains("Бухгалтерский учет, анализ и контроль"))
                    return "Бух. учет";
                if (fullName.Contains("Программное обеспечение информационных технологий"))
                    return "ПОИТ";
                if (fullName.Contains("Розничные услуги в банке"))
                    return "Розн. услуги";
                if (fullName.Contains("Планово-экономическая"))
                    return "План.-экон.";
                if (fullName.Contains("Банковское дело"))
                    return "Банк. дело";
                if (fullName.Contains("Торговая деятельность"))
                    return "Торг. деятель.";
                if (fullName.Contains("Логистическая"))
                    return "Логистика";

                // Общий случай: берем первые 20 символов
                return fullName.Length > 20 ? fullName.Substring(0, 20) + "..." : fullName;
            }

            return fullName;
        }

        private void AllOlimpsLink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Переход на страницу олимпиад
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.LoadPage("Olympiads");
        }

        private void AllStudentsLink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Переход на страницу учащихся
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.LoadPage("Students");
        }

        // Вспомогательные классы
        private class MonthData
        {
            public string MonthName { get; set; }
            public int MonthNumber { get; set; }
            public int Year { get; set; }
            public int OlimpCount { get; set; }
        }

        private class RatingItem
        {
            public int Position { get; set; }
            public string FullName { get; set; }
            public int OlimpCount { get; set; }
            public int Course { get; set; }
            public string Group { get; set; }
        }
    }
}