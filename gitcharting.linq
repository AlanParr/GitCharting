<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Drawing.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.DataVisualization.dll</Reference>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>System.Windows.Forms.DataVisualization</Namespace>
  <Namespace>System.Windows.Forms.DataVisualization.Charting</Namespace>
</Query>

//PreRequisites:
//You'll need System.Windows.Forms.DataVisualizations. This is in .net4.

//USAGE:
//Export git log using command:  git log --stat >> c:\commitlog.txt.
//Run.
void Main()
{
	var logFile = @"C:\commitlog.txt";
	var foo = new List<Commit>();
	var thisCommit = new Commit();
	
	foreach (var line in File.ReadAllLines(logFile))
	{
		if (line.StartsWith("commit "))
		{
			thisCommit = new Commit();
			thisCommit.CommitHash = line.Substring("commit ".Length);
		}
		
		if(line.StartsWith("Author: "))
		{
			thisCommit.AddAuthorString(line.Substring("Author: ".Length));
		}
		
		if(line.StartsWith("Date: "))
		{
			var commitDateString = line.Substring("Date: ".Length).Trim();
			commitDateString = commitDateString.Substring(0,commitDateString.Length-6).Trim();
			thisCommit.CommitDate = DateTime.ParseExact(commitDateString,"ddd MMM d HH:mm:ss yyyy",CultureInfo.InvariantCulture);
		}
		
		if(line.StartsWith("  "))
		{
			thisCommit.Message = line.Trim();
		}
		
		if(line.Contains("insertions"))
		{
			thisCommit.AddChangeString(line.Trim());
			foo.Add(thisCommit);
		}
	}

		var authorStats = from i in foo
				where i.CommitHash != "e73ce077652ce77b7f79eb3de4f8b65e246cbca5" && i.CommitHash != "b510dd93ee1591efe100351c64e9fa1726f8f1ad"
				group i by i.AuthorName into g
				select new{Author = g.Key, 
							Commits = g.Count (),
							Insertions = g.Sum (x => x.Insertions),
						    Deletions = g.Sum (x => x.Deletions),
							FilesChanged = g.Sum (x => x.FilesChanged),
							DelPct = Utilities.Percentage(g.Sum (x => x.Insertions),g.Sum (x => x.Deletions))};
	
	authorStats.Dump();
	
	var stats = from i in foo
				where i.CommitDate >= DateTime.Today.AddDays(-60)
				group i by i.CommitDate.Date into g
				select new{CommitDate = g.Key, 
							Commits = g.Count (),
							Insertions = g.Sum (x => x.Insertions),
						    Deletions = g.Sum (x => x.Deletions),
							FilesChanged = g.Sum (x => x.FilesChanged),
							DelPct = Utilities.Percentage(g.Sum (x => x.Insertions),g.Sum (x => x.Deletions))};
	
	stats.Dump();
	//var chartList = foo.Where (f => f.CommitDate >= DateTime.Today.AddDays(-10));
	var chartList = stats;
	// Chart must have a chart area, but it's not externally referenced later
	var chartArea1 = new ChartArea();
	var chart1 = new Chart();
	chart1.Width = 800;
	chart1.Height = 600;
	chart1.ChartAreas.Add(chartArea1);

	var yLabelStyle = new LabelStyle();
	yLabelStyle.Angle = 45;
	yLabelStyle.Font = new Font("Arial",8);
	yLabelStyle.IsStaggered=false;

	var range = 4000;
	
	var series1 = new Series();
	var series2 = new Series();
	
	var xs = chartList.Select (f => f.CommitDate.Date);
	var ys = chartList.Select (f => f.Insertions);
	var ys1 = chartList.Select (f => 0-f.Deletions);
	series1.Points.DataBindXY(xs.ToArray(), ys.ToArray());
	series2.Points.DataBindXY(xs.ToArray(), ys1.ToArray());
	chart1.Series.Add(series1);
	chart1.Series.Add(series2);
	
	series1.IsValueShownAsLabel=true;
	series2.IsValueShownAsLabel=true;
	
	chart1.ChartAreas[0].AxisY.Maximum = range;
	chart1.ChartAreas[0].AxisY.Minimum = -range;
	chart1.ChartAreas[0].AxisX.IsLabelAutoFit=false;
	chart1.ChartAreas[0].AxisX.LabelAutoFitStyle = LabelAutoFitStyles.None;
	chart1.ChartAreas[0].AxisX.LabelStyle = yLabelStyle;

	var b = new Bitmap(width: chart1.Width+100, height: chart1.Height+100);
	chart1.DrawToBitmap(b, chart1.Bounds);
	b.Dump();
}

// Define other methods and classes here
public class Commit
{
	private string _authorString = "";
	private string _authorName = "";
	private string _authorEmail = "";
	
	private string _changeString = "";
	private int _filesChanged = 0;
	private int _insertions = 0;
	private int _deletions = 0;

	public string CommitHash {get;set;}
	
	public DateTime CommitDate {get;set;}
	public string Message {get;set;}
	
	public void AddChangeString(string value) {
			_changeString = value;
			var elements = value.Split(' ');
			_filesChanged = int.Parse(elements[0].Trim());
			_insertions = int.Parse(elements[3].Trim());
			_deletions = int.Parse(elements[5].Trim());
	}
	public void AddAuthorString(string value)
	{
		var elements = value.Split('<');
		_authorName = elements[0].Trim();
		_authorEmail = elements[1].Replace(">",string.Empty).Trim();
		if(_authorName.Length<3)
		{
			_authorName = _authorName.ToUpper();
		}
	}
	
	//public string ChangeString{get{return _changeString;}}
	public int FilesChanged {get{return _filesChanged;}}
	public int Insertions {get{return _insertions;}}
	public int Deletions {get{return _deletions;}}
	
	public string AuthorName {get{return _authorName;}}
	public string AuthorEmail {get{return _authorEmail;}}
	
}

public static class Utilities
{
	public static decimal Percentage(int one, int two)
	{
		return Math.Round(Convert.ToDecimal(two) / Convert.ToDecimal(one)*100,0);
	}
}