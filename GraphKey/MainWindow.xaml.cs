using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Xml.Serialization;

namespace GraphKey
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IniSettings cfg;
        string cfgPath = "GraphKeyCfg.xml";

        Pattern currentPattern;
        Point previousCenter;
        bool registrationMode = false;
        bool delayMode = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            XmlSerializer cfgSerializer = new XmlSerializer(typeof(IniSettings));
            if (!File.Exists(cfgPath))
            {
                IniSettings newCfg = new IniSettings();

                // TODO: UserBase.Add
                Pattern test = new Pattern(5, 8);
                test.AddPoint(1, 2);
                test.AddPoint(1, 1);
                test.AddPoint(2, 1);
                test.AddPoint(2, 0);
                test.AddPoint(1, 0);
                test.AddPoint(0, 0);
                test.AddPoint(0, 1);

                UserInfo su = new UserInfo();
                su.Login = "Exide";
                su.PasswordHash = Pattern.GetHash(test);
                su.IsSuperUser = true;

                newCfg.UserBase.Add(su);

                using (FileStream fs = File.OpenWrite(cfgPath))
                    cfgSerializer.Serialize(fs, newCfg);
            }

            using (FileStream fs = File.OpenRead(cfgPath))
                this.cfg = (IniSettings)cfgSerializer.Deserialize(fs);

            // Очищаем всё, что было до этого для нового заполнения
            canvas.Children.Clear();
            ellipseGrid.Children.Clear();
            ellipseGrid.RowDefinitions.Clear();
            ellipseGrid.ColumnDefinitions.Clear();

            // Формирруем сетку
            for (int i = 0; i < cfg.RowCount; i++)
                ellipseGrid.RowDefinitions.Add(new RowDefinition());

            for (int i = 0; i < cfg.ColumnCount; i++)
                ellipseGrid.ColumnDefinitions.Add(new ColumnDefinition());            

            // Заполняем её кружками
            for (int i = 0; i < cfg.RowCount; i++)
            {
                for (int j = 0; j < cfg.ColumnCount; j++)
                {
                    // добавляем маленький круг
                    Ellipse smallEllipse = new Ellipse();
                    smallEllipse.Style = (Style)this.Resources["PreparedCircle"];

                    ellipseGrid.Children.Add(smallEllipse);
                    Grid.SetRow(smallEllipse, i);
                    Grid.SetColumn(smallEllipse, j);

                    // добавляем внешний круг, который изначально имеет толщину 0
                    Ellipse ellipse = new Ellipse();
                    ellipse.Style = (Style)this.Resources["SelectedCircle"];
                    ellipse.Tag = true; // флаг, чтобы отделять внешние круги от внутренних
                    ellipse.StrokeThickness = 0; // (Double)this.Resources["SelectedCircleThickness"];
                    ellipse.MouseEnter += MouseEnterHandler;
                    ellipse.MouseLeftButtonDown += MouseLeftButtonDownHandler;

                    ellipseGrid.Children.Add(ellipse);

                    Grid.SetRow(ellipse, i);
                    Grid.SetColumn(ellipse, j);
                }
            }

            // Обновим ресурсы, занеся новые размеры
            UpdateResources();
        }

        private void MouseEnterHandler(object sender, MouseEventArgs mouseEvtArgs)
        {
            if (currentPattern == null || delayMode) return;

            // Определяем x y и скармливаем в Pattern
            Ellipse ellipse = (Ellipse)sender;
            Point selectedPoint = GetGridCoordinates(ellipse);

            Point[] affectedPoints;
            if (currentPattern.TryAddPoint((int)selectedPoint.X, (int)selectedPoint.Y, out affectedPoints))
            {
                // Рендерим задетые точки
                foreach (var point in affectedPoints)
                    RenderNewPoint((int)point.X, (int)point.Y);
            }
        }

        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs mButtonEvtArgs)
        {
            if (loginBox.Text.Trim().Length == 0 || delayMode) return;

            currentPattern = new Pattern(cfg.RowCount, cfg.ColumnCount);

            var point = GetGridCoordinates((UIElement)sender);

            Point[] points;
            if (!currentPattern.TryAddPoint((int)point.X, (int)point.Y, out points) || points.Length != 1)
                throw new Exception($"Точка {point.X}:{point.Y} не была добавлена. Размер ключа: {currentPattern.Points.Length}");

            RenderNewPoint((int)point.X, (int)point.Y);
        }

        private void ellipseGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StopRecording();
        }

        private void ellipseGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            StopRecording();
        }

        private void ellipseGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentPattern == null && !delayMode)
                return;

            // Находим динамическую линию
            Line lineToRemove = null;
            foreach (Line line in canvas.Children.OfType<Line>())
                if (line.Uid == "DynamicLine")
                    lineToRemove = line;
            // И удаляем
            if (lineToRemove != null)
                canvas.Children.Remove(lineToRemove);

            // Если режим задержки, то только удаляем динамическую линию
            if (delayMode) return;

            Point relativePoint = e.GetPosition(canvas);
            Line dynamicLine = new Line();
            dynamicLine.Uid = "DynamicLine";
            dynamicLine.X1 = previousCenter.X;
            dynamicLine.Y1 = previousCenter.Y;
            dynamicLine.X2 = relativePoint.X;
            dynamicLine.Y2 = relativePoint.Y;
            canvas.Children.Add(dynamicLine);
        }

        void RenderNewPoint(int x, int y)
        {
            foreach (UIElement child in ellipseGrid.Children)
            {
                int colIndex = Grid.GetColumn(child);
                int rowIndex = Grid.GetRow(child);

                if (x != colIndex || y != rowIndex) continue;

                Ellipse ellipse = child as Ellipse;
                if (ellipse.Tag == null || (bool)ellipse.Tag != true) continue;

                (child as Ellipse).StrokeThickness = (Double)this.Resources["SelectedCircleThickness"];

                // Получаем все точки в текущем ключе
                if (currentPattern.Count == 1)
                    previousCenter = GetCenterPoint(ellipse);
                else
                {
                    Line line = new Line();

                    line.X1 = previousCenter.X;
                    line.Y1 = previousCenter.Y;

                    Point newCenter = GetCenterPoint(ellipse);
                    line.X2 = newCenter.X;
                    line.Y2 = newCenter.Y;
                    canvas.Children.Add(line);

                    previousCenter = newCenter;
                }              
                break;
            }
        }

        async void StopRecording()
        {
            if (currentPattern == null) return;

            // Узнаем логин и хеш введенного пароля
            string login = loginBox.Text.Trim();
            string patternHash = Pattern.GetHash(currentPattern);
            int pointCount = currentPattern.Count;
            currentPattern = null;

            // Кисть, которая вычисляется далее и будет 
            // использована при временной окраске кружков
            SolidColorBrush tempBrush = null;

            if (registrationMode)
            { // если регистрируется новый пользователь
                if (cfg.UserBase.Any(u => u.Login == login))
                {
                    System.Windows.MessageBox.Show("Пользователь уже есть в базе");
                    //tip.Text = "Пользователь уже есть в базе";
                    tempBrush = (SolidColorBrush)this.Resources["DeniedColor"];
                }
                else if (pointCount < 4)
                {
                    System.Windows.MessageBox.Show("Длина ключа должна быть не меньше 4");
                    //tip.Text = "Длина ключа должна быть не меньше 4";
                    tempBrush = (SolidColorBrush)this.Resources["DeniedColor"];
                }
                else
                {
                    cfg.UserBase.Add(new UserInfo { Login = login, PasswordHash = patternHash });
                    //tip.Text = "Пользователь добавлен в базу";
                    tempBrush = (SolidColorBrush)this.Resources["RegistratedColor"];

                    File.Delete(cfgPath);
                    using (FileStream fs = File.OpenWrite(cfgPath))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(IniSettings));
                        ser.Serialize(fs, cfg);
                    }
                }                
            }
            else
            { // если попытка ввести пароль, соответствующий логину
                UserInfo targetUser = cfg.UserBase.FirstOrDefault(u => u.Login == login);

                if (targetUser != null && targetUser.PasswordHash == patternHash)
                {
                    // Успешный вход, проверяем права и определяем состояние кнопки регистрации
                    if (targetUser.IsSuperUser)
                        regButton.Visibility = Visibility.Visible;
                    else
                        regButton.Visibility = Visibility.Collapsed;

                    tempBrush = (SolidColorBrush)this.Resources["GrantedColor"];
                }
                else
                {
                    regButton.Visibility = Visibility.Collapsed;
                    tempBrush = (SolidColorBrush)this.Resources["DeniedColor"];
                }
            }

            // Из полученной кисти формируем кисть для линий с учетом прозрачности
            SolidColorBrush lineBrush = new SolidColorBrush(tempBrush.Color);
            foreach (Line line in canvas.Children.OfType<Line>())
            {
                line.Stroke = lineBrush;
            }

            // Меняем на время цвета кружков
            foreach (UIElement child in ellipseGrid.Children)
            {
                Ellipse ell = child as Ellipse;

                if (ell.Tag == null || (bool)ell.Tag == false) continue;

                Brush brush = tempBrush;
                ell.Stroke = brush;
            }

            delayMode = true; // выставляем флаг, запрещающий рендерить события
            await Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1500);
            });

            foreach (UIElement child in ellipseGrid.Children)
            {
                Ellipse ellipse = child as Ellipse;

                // Определяем внутренний это круг или внешний
                if (ellipse.Tag != null && (bool)ellipse.Tag == true)
                {
                    // Делаем обратно зеленым и скрываем, если это внешний
                    ellipse.StrokeThickness = 0;
                    ellipse.Stroke = (SolidColorBrush)this.Resources["SelectedCircleColor"];
                }
            }
            canvas.Children.Clear();
            delayMode = false;
            registrationMode = false;
            regButton.IsEnabled = true;
        }

        Point GetGridCoordinates(UIElement element)
        {
            int x = Grid.GetColumn(element);
            int y = Grid.GetRow(element);
            return new Point(x, y);
        }

        Point GetCenterPoint(UIElement element)
        {
            Point relativeCenter = new System.Windows.Point(
                element.DesiredSize.Width / 2,
                element.DesiredSize.Height / 2);

            return element.TranslatePoint(relativeCenter, ellipseGrid);
        }

        UIElement GetElementByCoordinates(int x, int y)
        {
            foreach (UIElement child in ellipseGrid.Children)
            {
                int colIndex = Grid.GetColumn(child);
                int rowIndex = Grid.GetRow(child);

                if (x != colIndex || y != rowIndex) continue;

                Ellipse ellipse = child as Ellipse;
                if (ellipse.Tag != null && (bool)ellipse.Tag == true)
                    return child;
            }
            return null;
        }

        void UpdateResources()
        {
            if (cfg == null) return;

            var gridSize = ellipseGrid.RenderSize;
            double rowLength = gridSize.Height / cfg.RowCount;
            double colLength = gridSize.Width / cfg.ColumnCount;

            double least = rowLength > colLength ? colLength : rowLength;

            this.Resources["SelectedCircleDiameter"] = least * 0.75;
            this.Resources["PreparedCircleDiameter"] = least * 0.15;

            this.Resources["LineThickness"] = least * 0.07;
            this.Resources["SelectedCircleThickness"] = least * 0.07;
            this.Resources["PreparedCircleThickness"] = least * 0.03;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateResources();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            registrationMode = true;
            //System.Windows.MessageBox.Show("Введите ключ для регистрации");
            //tip.Text = "Введите ключ для регистрации";
            regButton.IsEnabled = false;
        }
    }
}
