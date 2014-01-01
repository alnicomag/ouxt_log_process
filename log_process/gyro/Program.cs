using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpPlot;
using System.Diagnostics;
using System.Threading;

namespace gyro
{
	class Program
	{
		static Program()
		{
			stInputDir = Environment.CurrentDirectory + "\\..\\..\\rawdata";
			stOutputDir = Environment.CurrentDirectory + "\\..\\..\\out";
		}

		private static string stInputDir;
		private static string stOutputDir;

		private static List<SixAxisLog> logdata = new List<SixAxisLog>();

		private static readonly double QUARRY_SPAN = 200;		// データ切り出し間隔[秒]

		static void Main(string[] args)
		{
			List<string> filefullpaths = Directory.EnumerateFiles(stInputDir, "*.csv", SearchOption.TopDirectoryOnly).ToList();

			foreach (string item in filefullpaths)
			{
				logdata.Add(new SixAxisLog());
				int index = logdata.Count - 1;
				using (StreamReader sr = new StreamReader(item))
				{
					string line = sr.ReadLine();
					logdata[index].Lable = line.Substring(1, line.Length - 1);
					while (!sr.EndOfStream)
					{
						line = sr.ReadLine();
						string[] div_line = line.Split(',');
						logdata[index].SetRecord(ParseDateTime(div_line[0]), Double.Parse(div_line[1]), Double.Parse(div_line[2]), Double.Parse(div_line[3]));
					}
				}
				logdata[index].OffsetTime(ParseDateTime("2013-12-28 15:18:19.408600"));
			}
			
			Debug.Assert(logdata[0].Lable == "left", "データ名称が異なります");
			Debug.Assert(logdata[1].Lable == "right", "データ名称が異なります");
			List<string> left = WriteLog(logdata[0]);
			List<string> right = WriteLog(logdata[1]);
			
			DrawDivGraph(right,left);
			DrawAllGraph(logdata[1].Lable + ".txt", logdata[0].Lable + ".txt");


		}

		static double ParseDateTime(string arg)
		{
			string[] date_time = arg.Split(' ');
			string date = date_time[0];		// 日は取り敢えず無視
			string[] h_m_s = date_time[1].Split(':');	// 時間，分，秒

			double second = Double.Parse(h_m_s[2]) + Double.Parse(h_m_s[1]) * 60 + Double.Parse(h_m_s[0]) * 60 * 60;	// 秒単位に

			return second;
		}

		/// <summary>
		/// gnuplotに渡すためにデータをファイルに書き出す．一定の時間ごとにデータを分割する．
		/// </summary>
		/// <param name="log"></param>
		/// <returns>分割されたデータに対して命名された全てのファイル名（拡張子含む）を返す</returns>
		private static List<string> WriteLog(SixAxisLog log)
		{
			if (!Directory.Exists(stOutputDir))
			{
				Directory.CreateDirectory(stOutputDir);
			}

			using (StreamWriter sw = new StreamWriter(Path.Combine(stOutputDir, log.Lable + ".txt")))
			{
				sw.WriteLine("#{0}\t{1}\t{2}\t{3}", "time[s]", "yaw[degree]", "pitch[degree]", "roll[degree]");
				for (int i = 0; i < log.Count; i++)
				{
					var record = log.GetRecord(i);
					sw.WriteLine("{0:f6}\t{1:e3}\t{2:e3}\t{3:e3}", record.Item1, record.Item2, record.Item3, record.Item4);
				}
			}

			List<SixAxisLog> divlog = new List<SixAxisLog>();
			int loop = 0;
			SixAxisLog temp;
			while ((temp = log.RecordQuarry(log.Lable + "_" + loop.ToString(), QUARRY_SPAN * loop, QUARRY_SPAN * (loop + 1))) != null)
			{
				divlog.Add(temp);
				loop++;
			}

			List<string> filenames = new List<string>();
			for (int i = 0; i < divlog.Count; i++)
			{
				filenames.Add(divlog[i].Lable + ".txt");
				using (StreamWriter sw = new StreamWriter(Path.Combine(stOutputDir, divlog[i].Lable + ".txt")))
				{
					sw.WriteLine("#{0}\t{1}\t{2}\t{3}", "time[s]", "yaw[degree]", "pitch[degree]", "roll[degree]");
					for (int j = 0; j < divlog[i].Count; j++)
					{
						var record = divlog[i].GetRecord(j);
						sw.WriteLine("{0:f6}\t{1:e3}\t{2:e3}\t{3:e3}", record.Item1, record.Item2, record.Item3, record.Item4);
					}
				}
			}
			return filenames;
		}

		private static void DrawAllGraph(string data_filename_r, string data_filename_l)
		{
			string fig_filename = "6AxisLog.eps";
			string data_file_path_r = Path.Combine(stOutputDir, data_filename_r);
			string data_file_path_l = Path.Combine(stOutputDir, data_filename_l);

			#region gnuplot
			using (PlotStream gnuplot = new PlotStream())
			{
				gnuplot.Start();
				gnuplot.ChangeDirectory(stOutputDir);
				gnuplot.SetLineStyle(1, new LineStyle(1, 1, Color.Cyan,7,2));
				gnuplot.SetLineStyle(2, new LineStyle(1, 1, Color.Green, 7, 2));
				gnuplot.SetLineStyle(3, new LineStyle(1, 1, Color.Magenta, 7, 2));
				gnuplot.SetLineStyle(4, new LineStyle(1, 1, Color.Orange, 7, 2));

				gnuplot.SetLegendPosition(LegendPosition.left, LegendPosition.top);
				gnuplot.SetLegendFont(24, "Helvetica");

				gnuplot.SetXLabel("time [s]", 36, "Helvetica");
				gnuplot.SetYLabel("angle[degrees]", 36, "Helvetica");

				gnuplot.SetTicsFont(28, "Helvetica");
				gnuplot.SetXRange(0, 3200);
				gnuplot.SetXTics(0, 400, false);
				gnuplot.SetYRange(-20, 20);
				gnuplot.SetYTics(-20, 2);

				gnuplot.SetMargin(7.5, 10.5, 1, 3.5);
				gnuplot.SetGrid();

				gnuplot.PlotFromFile(data_file_path_r, 1, 3, PlottingStyle.lines, 1, PlotAxis.x1y1, "right pitch");
				gnuplot.ReplotFromFile(data_file_path_r, 1, 4, PlottingStyle.lines, 2, PlotAxis.x1y1, "right roll");
				gnuplot.ReplotFromFile(data_file_path_l, 1, 3, PlottingStyle.lines, 3, PlotAxis.x1y1, "left pitch");
				gnuplot.ReplotFromFile(data_file_path_l, 1, 4, PlottingStyle.lines, 4, PlotAxis.x1y1, "left roll");

				gnuplot.SetTerminal(Terminal.postscript, 20, 14, 28, "Helvetica");
				gnuplot.SetOutput(fig_filename);
				gnuplot.Replot();
				gnuplot.SetOutput();
				gnuplot.Stream.WriteLine("unset style line");
				gnuplot.Stream.WriteLine("set terminal wxt");
			}
			Thread.Sleep(1000);
			#endregion
		}

		private static void DrawDivGraph(List<string> data_filenames_r, List<string> data_filenames_l)
		{
			int num = Math.Max(data_filenames_r.Count, data_filenames_l.Count);

			string fig_filename;
			for (int i = 0; i < num; i++)
			{
				string data_file_path_r = Path.Combine(stOutputDir, data_filenames_r[i]);
				string data_file_path_l = Path.Combine(stOutputDir, data_filenames_l[i]);
				double x_range_start = QUARRY_SPAN * i;
				double x_range_end = QUARRY_SPAN * (i+1);

				fig_filename = "6AxisLog_Pitch_" + i.ToString() + ".eps";
				#region gnuplot_pitch
				using (PlotStream gnuplot = new PlotStream())
				{
					gnuplot.Start();
					gnuplot.ChangeDirectory(stOutputDir);
					gnuplot.SetLineStyle(1, new LineStyle(1, 1, Color.Cyan, 6, 0.1));
					gnuplot.SetLineStyle(2, new LineStyle(1, 1, Color.Magenta, 6, 0.1));

					gnuplot.SetLegendPosition(LegendPosition.left, LegendPosition.top);
					gnuplot.SetLegendFont(24, "Helvetica");
					//gnuplot.SetLegendTitle("Pitch comparison");

					gnuplot.SetXLabel("time [s]", 36, "Helvetica");
					gnuplot.SetYLabel("angle[degrees]", 36, "Helvetica");

					gnuplot.SetTicsFont(28, "Helvetica");
					gnuplot.SetXRange(x_range_start, x_range_end);
					gnuplot.SetXTics(x_range_start, QUARRY_SPAN / 10, false);
					gnuplot.SetYRange(-18, 6);
					gnuplot.SetYTics(-18, 2);

					gnuplot.SetMargin(7.5, 10.5, 1, 3.5);
					gnuplot.SetGrid();

					gnuplot.PlotFromFile(data_file_path_r, 1, 3, PlottingStyle.lines, 1, PlotAxis.x1y1, "right pitch");
					gnuplot.ReplotFromFile(data_file_path_l, 1, 3, PlottingStyle.lines, 2, PlotAxis.x1y1, "left pitch");

					gnuplot.SetTerminal(Terminal.postscript, 20, 14, 28, "Helvetica");
					gnuplot.SetOutput(fig_filename);
					gnuplot.Replot();
					gnuplot.SetOutput();
					gnuplot.Stream.WriteLine("unset style line");
					gnuplot.Stream.WriteLine("set terminal wxt");
				}
				Thread.Sleep(1000);
				#endregion
				

				fig_filename = "6AxisLog_Roll_" + i.ToString() + ".eps";
				#region gnuplot_roll
				using (PlotStream gnuplot = new PlotStream())
				{
					gnuplot.Start();
					gnuplot.ChangeDirectory(stOutputDir);
					gnuplot.SetLineStyle(1, new LineStyle(1, 1, Color.Green, 7, 2));
					gnuplot.SetLineStyle(2, new LineStyle(1, 1, Color.Orange, 7, 2));

					gnuplot.SetLegendPosition(LegendPosition.left, LegendPosition.top);
					gnuplot.SetLegendFont(24, "Helvetica");
					//gnuplot.SetLegendTitle("Roll comparison");

					gnuplot.SetXLabel("time [s]", 36, "Helvetica");
					gnuplot.SetYLabel("angle[degrees]", 36, "Helvetica");

					gnuplot.SetTicsFont(28, "Helvetica");
					gnuplot.SetXRange(x_range_start, x_range_end);
					gnuplot.SetXTics(x_range_start, QUARRY_SPAN / 10, false);
					gnuplot.SetYRange(-12, 12);
					gnuplot.SetYTics(-12, 2);

					gnuplot.SetMargin(7.5, 10.5, 1, 3.5);
					gnuplot.SetGrid();

					gnuplot.PlotFromFile(data_file_path_r, 1, 4, PlottingStyle.lines, 1, PlotAxis.x1y1, "right roll");
					gnuplot.ReplotFromFile(data_file_path_l, 1, 4, PlottingStyle.lines, 2, PlotAxis.x1y1, "left roll");

					gnuplot.SetTerminal(Terminal.postscript, 20, 14, 28, "Helvetica");
					gnuplot.SetOutput(fig_filename);
					gnuplot.Replot();
					gnuplot.SetOutput();
					gnuplot.Stream.WriteLine("unset style line");
					gnuplot.Stream.WriteLine("set terminal wxt");
				}
				Thread.Sleep(2000);
				#endregion
			}


			
		}
	}

	class SixAxisLog
	{
		public SixAxisLog()
		{
			time = new List<double>();
			yaw = new List<double>();
			pitch = new List<double>();
			roll = new List<double>();
		}

		public string Lable { get; set; }
		public int Count { get { return time.Count; } }

		public void SetRecord(double time, double yaw,double pitch,double roll)
		{
			this.time.Add(time);
			this.yaw.Add(yaw);
			this.pitch.Add(pitch);
			this.roll.Add(roll);
		}

		public Tuple<double, double, double, double> GetRecord(int index)
		{
			if (index >= time.Count)
			{
				throw new IndexOutOfRangeException();
			}
			return new Tuple<double, double, double, double>(time[index], yaw[index], pitch[index], roll[index]);
		}

		/// <summary>
		/// 開始時刻（含む）と終了時刻（含まない）を指定してデータを切り出す
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns>該当するデータが存在しない場合はnullを返す</returns>
		public SixAxisLog RecordQuarry(string newlabel, double start, double end)
		{
			var ret = new SixAxisLog();
			ret.Lable = newlabel;
			for (int i = 0; i < Count; i++)
			{
				if ((start <= time[i]) && (time[i] < end))
				{
					ret.SetRecord(time[i], yaw[i], pitch[i], roll[i]);
				}
			}
			if (ret.Count == 0)
			{
				return null;
			}
			return ret;
		}

		public void OffsetTime()
		{
			if (time.Count == 0)
			{
				throw new InvalidOperationException();
			}

			double offset = time[0];
			for (int i = 0; i < time.Count; i++)
			{
				time[i] -= offset;
			}
		}

		public void OffsetTime(double zero)
		{
			if (time.Count == 0)
			{
				throw new InvalidOperationException();
			}

			for (int i = 0; i < time.Count; i++)
			{
				time[i] -= zero;
			}
		}

		private List<double> time;
		private List<double> yaw;
		private List<double> pitch;
		private List<double> roll;
	}
}
