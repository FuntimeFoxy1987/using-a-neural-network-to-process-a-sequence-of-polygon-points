
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using static System.Net.WebRequestMethods;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Test
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    class Resalt
    {
        public int[][] points { get; set; }
        public List<int> sequence { get; set; }
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            List<List<Point3D>> lists = new List<List<Point3D>>();
            using (FileStream fstream = new FileStream("test", FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fstream.Length];
                fstream.Read(buffer, 0, buffer.Length);
                string res_string = Encoding.Default.GetString(buffer);
                Regex reg = new Regex(@"\n");
                string[] data = reg.Split(res_string);
                for (int i = 0; i < data.Length - 1; i++)
                {
                    lists.Add(JsonSerializer.Deserialize<List<Point3D>>(data[i]));
                }
            }
            Console.WriteLine(lists.Count);

            int time = (DateTime.Now.Hour * 3600000) + (DateTime.Now.Minute * 60000) + (DateTime.Now.Second * 1000) + DateTime.Now.Millisecond;
            for (int k = 0; k < lists.Count; k++)
            {
                List<Point3D> list = lists[k];
                Sort1(list);
            }
            time = ((DateTime.Now.Hour * 3600000) + (DateTime.Now.Minute * 60000) + (DateTime.Now.Second * 1000) + DateTime.Now.Millisecond) - time;
            Console.WriteLine($"time: {time}ms");

            time = (DateTime.Now.Hour * 3600000) + (DateTime.Now.Minute * 60000) + (DateTime.Now.Second * 1000) + DateTime.Now.Millisecond;
            for (int k = 0; k < lists.Count; k++)
            {
                List<Point3D> list = lists[k];
                Sort2(list);
            }
            time = ((DateTime.Now.Hour * 3600000) + (DateTime.Now.Minute * 60000) + (DateTime.Now.Second * 1000) + DateTime.Now.Millisecond) - time;
            Console.WriteLine($"time: {time}ms");
        }
        internal List<Point> Get_2D(List<Point3D> list)
        {
            double xy = 0.0f, xz = 0.0f, yz = 0.0f;
            for (int i = 0; i < list.Count; i += 2)
            {
                int l = 0;
                if (i + 2 != 4)
                {
                    l = i + 2;
                }

                xy += Math.Sqrt(Math.Pow(list[i + 1].X - list[i].X, 2) + Math.Pow(list[i + 1].Y - list[i].Y, 2)) * Math.Sqrt(Math.Pow(list[l].X - list[i + 1].X, 2) + Math.Pow(list[l].Y - list[i + 1].Y, 2));
                xz += Math.Sqrt(Math.Pow(list[i + 1].Z - list[i].Z, 2) + Math.Pow(list[i + 1].X - list[i].X, 2)) * Math.Sqrt(Math.Pow(list[l].Z - list[i + 1].Z, 2) + Math.Pow(list[l].X - list[i + 1].X, 2));
                yz += Math.Sqrt(Math.Pow(list[i + 1].Z - list[i].Z, 2) + Math.Pow(list[i + 1].Y - list[i].Y, 2)) * Math.Sqrt(Math.Pow(list[l].Z - list[i + 1].Z, 2) + Math.Pow(list[l].Y - list[i + 1].Y, 2));
            }

            int type = 0;
            if (yz > xz)
            {
                if (yz > xy)
                {
                    type = 2;
                }
            }
            else
            {
                if (xz > xy)
                {
                    type = 1;
                }
            }
            List<Point> list_2D = new List<Point>();
            switch (type)
            {
                case 0:
                    for (int i = 0; i < list.Count; i++) { list_2D.Add(new Point { X = list[i].X, Y = list[i].Y }); }
                    break;
                case 1:
                    for (int i = 0; i < list.Count; i++) { list_2D.Add(new Point { X = list[i].X, Y = list[i].Z }); }
                    break;
                case 2:
                    for (int i = 0; i < list.Count; i++) { list_2D.Add(new Point { X = list[i].Z, Y = list[i].Y }); }
                    break;
                default:
                    break;
            }
            return list_2D;
        }
        internal List<int[]> Quantization_points(List<Point> points)
        {
            int length = points.Count;
            List<double> xy_from_points = new List<double>();
            for (int i = 0; i < length; i++)
            {
                if (xy_from_points.IndexOf(points[i].X) == -1) { xy_from_points.Add(points[i].X); }
                if (xy_from_points.IndexOf(points[i].Y) == -1) { xy_from_points.Add(points[i].Y); }
            }
            xy_from_points.Sort();
            List<int[]> q_points = new List<int[]>();
            for (int i = 0; i < length; i++) { q_points.Add(new int[] { xy_from_points.IndexOf(points[i].X) + 1, xy_from_points.IndexOf(points[i].Y) + 1 }); }
            return q_points;
        }
        internal bool Normal_order_points(List<int[]> points)
        {
            int length = points.Count;
            int[][][] _lines = new int[length][][];
            for (int i = 0; i < length; i++)
            {
                if (i + 1 != length) { _lines[i] = new int[][] { points[i], points[i + 1] }; }
                else { _lines[i] = new int[][] { points[i], points[0] }; }
            }
            int x_max = 0, y_max = 0;
            for (int i = 0; i < length; i++)
            {
                if (x_max < points[i][0]) { x_max = points[i][0]; }
                if (y_max < points[i][1]) { y_max = points[i][1]; }
            }
            for (int i = 0; i < length; i++)
            {
                for (int j = i + 1; j < length; j++)
                {
                    int a = _lines[i][1][0] - _lines[i][0][0];
                    int b = _lines[i][1][1] - _lines[i][0][1];
                    int c = _lines[j][1][0] - _lines[j][0][0];
                    int f = _lines[j][1][1] - _lines[j][0][1];
                    double x = 0, y = 0;
                    if (c != 0 && f != 0)
                    {
                        double t1 = (_lines[i][0][1] / f) - (_lines[j][0][1] / f) - (_lines[i][0][0] / c) + (_lines[j][0][0] / c);
                        double t2 = (a / c) - (b / f);
                        if (t2 != 0)
                        {
                            double t = t1 / t2;
                            x = t * (a) + _lines[i][0][0];
                            y = t * (b) + _lines[i][0][1];
                        }
                    }
                    if (c == 0 && f != 0)
                    {
                        if (a != 0)
                        {
                            x = _lines[j][1][0];
                            y = ((b / a) * (x - _lines[i][0][0])) - _lines[i][0][1];
                        }
                        else { continue; }
                    }
                    if (c != 0 && f == 0)
                    {
                        if (b != 0)
                        {
                            y = _lines[j][1][1];
                            x = ((a / b) * (y - _lines[i][0][1])) - _lines[i][0][0];
                        }
                        else { continue; }
                    }
                    //Console.WriteLine($"[{x};{y}]");
                    if (x <= x_max && y <= y_max && x >= 0 && y >= 0 && ((Math.Round(x, 3) % (int)x != 0.0 && Math.Round(y, 3) % (int)y != 0.0) || (points.ToList<int[]>().FindIndex(p => p[0] == (int)x && p[1] == (int)y) == -1))) { return false; }
                }
            }
            return true;
        }
        internal Resalt Repation_output_points(int[][] old_points, int[][] points)
        {
            int length = points.Length;
            Resalt resalt = new Resalt() { points = points, sequence = new List<int>() };
            for (int i = 0; i < length; i++)
            {
                resalt.sequence.Add(points.ToList<int[]>().FindIndex(p => p[0] == old_points[i][0] && p[1] == old_points[i][1]) + 1);
            }
            Point min_point = new Point() { X = 1000, Y = 1000 };
            for (int i = 0; i < points.Length; i++)
            {
                if (min_point.X >= points[i][0] && min_point.Y >= points[i][1] && points[i][0] <= points[i][1])
                {
                    min_point.X = points[i][0];
                    min_point.Y = points[i][1];
                }
            }
            int index_min_point = points.ToList<int[]>().FindIndex(p => p[0] == (int)min_point.X && p[1] == (int)min_point.Y);
            bool invers = false;
            if (index_min_point + 1 != length)
            {
                for (int i = 0; i < length; i++)
                {
                    if (i != index_min_point && points[i][1] > points[index_min_point + 1][1]) { invers = true; }
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (i != index_min_point && points[i][1] > points[0][1]) { invers = true; }
                }
            }
            if (index_min_point != 0)
            {
                for (int i = 0; i < length; i++)
                {
                    if (resalt.sequence[i] - index_min_point < 0)
                    {
                        resalt.sequence[i] = length + (resalt.sequence[i] - index_min_point);
                    }
                    else
                    {
                        if (resalt.sequence[i] - index_min_point != 0)
                        {
                            resalt.sequence[i] = resalt.sequence[i] - index_min_point;
                        }
                        else
                        {
                            resalt.sequence[i] = length;
                        }
                    }
                }
            }
            if (invers)
            {
                for (int i = 1, l = length - 1; i < length; i++, l--)
                {
                    resalt.sequence[resalt.sequence.IndexOf(i + 1)] = l + 1;
                }
            }
            //Console.WriteLine($"{index_min_point}; {invers}");
            for (int i = 0; i < length; i++)
            {
                resalt.points[i] = old_points[resalt.sequence.IndexOf(i + 1)];
            }
            return resalt;
        }
        internal List<int> Sort2(List<Point3D> list)
        {
            int length = list.Count;
            List<int[]> q_points = Quantization_points(Get_2D(list));
            List<int[]> q_points_sort = new List<int[]>();
            List<int> sequence = new List<int>();
            for (int i = 0; i < length; i++)
            {
                q_points_sort.Add(new int[] { 0, 0 });
            }
            if (!Normal_order_points(q_points))
            {
                Point min_point = new Point() { X = 1000, Y = 1000 };
                for (int i = 0; i < length; i++)
                {
                    if (min_point.X >= q_points[i][0] && min_point.Y >= q_points[i][1] && q_points[i][0] <= q_points[i][1])
                    {
                        min_point.X = q_points[i][0];
                        min_point.Y = q_points[i][1];
                    }
                }
                int index_min_point = q_points.ToList<int[]>().FindIndex(p => p[0] == (int)min_point.X && p[1] == (int)min_point.Y);
                List<double> angles = new List<double>();
                for (int i = index_min_point + 1; i < length + index_min_point; i++)
                {
                    if (i < length)
                    {
                        double l1 = q_points[i][0] - min_point.X, l2 = Math.Sqrt(Math.Pow(q_points[i][0] - min_point.X, 2) + Math.Pow(q_points[i][1] - min_point.Y, 2));
                        angles.Add(Math.Acos(l1 / l2) * 180 / Math.PI);
                    }
                    else
                    {
                        double l1 = q_points[i - length][0] - min_point.X, l2 = Math.Sqrt(Math.Pow(q_points[i - length][0] - min_point.X, 2) + Math.Pow(q_points[i - length][1] - min_point.Y, 2));
                        angles.Add(Math.Acos(l1 / l2) * 180 / Math.PI);
                    }
                }
                angles.Sort();
                q_points_sort[0] = new int[2] { (int)min_point.X, (int)min_point.Y };
                for (int i = index_min_point + 1; i < length + index_min_point; i++)
                {
                    if (i < q_points.Count)
                    {
                        double l1 = q_points[i][0] - min_point.X, l2 = Math.Sqrt(Math.Pow(q_points[i][0] - min_point.X, 2) + Math.Pow(q_points[i][1] - min_point.Y, 2));
                        q_points_sort[angles.IndexOf(Math.Acos(l1 / l2) * 180 / Math.PI) + 1] = new int[] { q_points[i][0], q_points[i][1] };
                    }
                    else
                    {
                        double l1 = q_points[i - length][0] - min_point.X, l2 = Math.Sqrt(Math.Pow(q_points[i - length][0] - min_point.X, 2) + Math.Pow(q_points[i - length][1] - min_point.Y, 2));
                        q_points_sort[angles.IndexOf(Math.Acos(l1 / l2) * 180 / Math.PI) + 1] = new int[] { q_points[i - length][0], q_points[i - length][1] };
                    }
                }
                for (int i = 0; i < length; i++)
                {
                    sequence.Add(q_points_sort.FindIndex(p => p[0] == q_points[i][0] && p[1] == q_points[i][1]) + 1);
                }
                return sequence;
            }
            for (int i = 0; i < length; i++)
            {
                sequence.Add(i + 1);
            }
            return sequence;
        }
        internal List<int> Sort1(List<Point3D> list)
        {
            int length = list.Count;
            int[][] q_points = Quantization_points(Get_2D(list));
            int[][] q_points_old = Quantization_points(Get_2D(list));
            List<int> sequence = new List<int>();
            int intd = 0;
            while (!Normal_order_points(q_points))
            {
                intd++;
                int[][] q_points_r = q_points;
                Random random = new Random();
                for (int i = 0; i < q_points.Length; i++)
                {
                    int[] old = q_points[i];
                    int index = random.Next(i, q_points.Length);
                    q_points_r[i] = q_points[index];
                    q_points_r[index] = old;
                }
                q_points = q_points_r;
                if (intd == 100)
                {
                    break;
                }
            }
            if (intd != 100)
            {
                for (int i = 0; i < length; i++)
                {
                    sequence.Add(q_points.FindIndex(p => p[0] == q_points_old[i][0] && p[1] == q_points_old[i][1]) + 1);
                }
                return sequence;
            }
            return sequence;
        }
    }
}
