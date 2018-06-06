using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using StoreCrew.Utils.Security;

namespace GraphKey
{
    class Pattern
    {
        List<Point> points = new List<Point>();
        int height, width;

        public Point[] Points => points.ToArray();
        public int Count => points.Count;
        public int Height => height;
        public int Width => width;

        public Pattern(int height, int width)
        {
            this.height = height;
            this.width = width;
        }

        public void AddPoint(int x, int y)
        {
            Point[] points;

            if (!TryAddPoint(x, y, out points))
                throw new Exception("Что-то пошло не так при добавлении новой точки");
        }

        /// <summary>
        /// Попытка добавить новую точку в ключ. Если удачно - возвращается массив задетых по пути точек
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="affectedPoints"></param>
        /// <returns></returns>
        public bool TryAddPoint(int x, int y, out Point[] affectedPoints)
        {
            if (points.Count == 0) // если это первая точка
            {
                points.Add(new Point(x, y));
                affectedPoints = points.ToArray();
                return true;
            }
            else if (ContainsPoint(x,y)) // если точка уже добавлена
            {
                affectedPoints = null;
                return false;
            }
                        
            Point lastPoint = points.Last();
            Point newPoint = new Point(x, y);

            List<Point> affectedPointsList = new List<Point>();

            int deltaX = (int) Math.Abs(newPoint.X - lastPoint.X);
            int deltaY = (int) Math.Abs(newPoint.Y - lastPoint.Y);

            if (deltaX == 0 && deltaY == 0)
                throw new Exception("delta x|y = 0");

            // анонимный метод для добавления точки сразу в 2 перечисления
            Action<Point> add = (point) =>
            {
                this.points.Add(point);
                affectedPointsList.Add(point);
            };

            if (deltaX == deltaY) // точки на одной диагонали
            {
                int signedDeltaX = (int) (newPoint.X - lastPoint.X);
                int signedDeltaY = (int) (newPoint.Y - lastPoint.Y);

                int stepX, stepY;

                if (signedDeltaX > 0 && signedDeltaY > 0) // направо вниз
                {
                    stepX = stepY = 1;
                }
                else if (signedDeltaX > 0 && signedDeltaY < 0) // направо вверх
                {
                    stepX = 1;
                    stepY = -1;
                }
                else if (signedDeltaX < 0 && signedDeltaY > 0) // налево вниз
                {
                    stepX = -1;
                    stepY = 1;
                }
                else // налево вверх
                {
                    stepX = stepY = -1;
                }

                for (int i = 0; i < deltaX; i++)
                {
                    int nextX = (int) (lastPoint.X + (stepX * i) + (stepX > 0? 1: -1));
                    int nextY = (int) (lastPoint.Y + (stepY * i) + (stepY > 0? 1: -1));

                    Point nextPoint = new Point(nextX, nextY);

                    if (!ContainsPoint(nextPoint))
                        add(nextPoint);
                }
            }
            else if (deltaY == 0 || deltaX == 0) // точки на одной линии
            {
                if (deltaY == 0)
                {
                    for (int i = 0; i < deltaX; i++)
                    {
                        Point nextPoint = new Point(
                            newPoint.X > lastPoint.X ? lastPoint.X + 1 + i : lastPoint.X - 1 - i,
                            lastPoint.Y);

                        if (!ContainsPoint(nextPoint))
                            add(nextPoint);
                    }
                }
                else // deltaX = 0
                {
                    for (int i = 0; i < deltaY; i++)
                    {
                        Point nextPoint = new Point(
                            lastPoint.X,
                            newPoint.Y > lastPoint.Y ? lastPoint.Y + 1 + i : lastPoint.Y - 1 - i);

                        if (!ContainsPoint(nextPoint))
                            add(nextPoint);
                    }
                }
            }
            else // иначе точки надо соединять напрямую
            {
                add(new Point(x, y));
            }

            affectedPoints = affectedPointsList.ToArray();
            return affectedPoints.Length > 0;
        }

        bool ContainsPoint(int x, int y) => ContainsPoint(new Point(x,y));
        bool ContainsPoint(Point point) => this.points.Any(p => p.X == point.X && p.Y == point.Y);

        public static string GetHash(Pattern pattern)
        {
            List<byte> byteList = new List<byte>();

            Action<int> append = (num) =>
            {
                foreach (byte b in BitConverter.GetBytes(num))
                    byteList.Add(b);
            };

            append(pattern.height);
            append(pattern.width);
            
            foreach (Point point in pattern.Points)
            {
                append((int)point.X);
                append((int)point.Y);
            }

            return HexHashAssistant.ComputeHash(byteList.ToArray());
        }
    }
}
