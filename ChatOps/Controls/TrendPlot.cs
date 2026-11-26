using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ChatOps.Model;

namespace ChatOps.Controls;

/// <summary>Time-based plot. Missing measurements break a line rather than imply zero.</summary>
public sealed class TrendPlot : FrameworkElement
{
    public static readonly DependencyProperty SamplesProperty = DependencyProperty.Register(nameof(Samples),
        typeof(PointSample[]),typeof(TrendPlot),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsRender));
    public PointSample[]? Samples { get=>(PointSample[]?)GetValue(SamplesProperty);set=>SetValue(SamplesProperty,value); }
    public bool Percent { get; set; }
    protected override void OnRender(DrawingContext dc)
    {
        var samples=(Samples??[]).OrderBy(s=>s.At).ToArray();
        var brush=(Brush)FindResource("TextSecondary");
        void Label(string value,double x,double y) => dc.DrawText(new FormattedText(value,CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,brush,VisualTreeHelper.GetDpi(this).PixelsPerDip),new Point(x,y));
        double w=Math.Max(1,ActualWidth-68), h=Math.Max(1,ActualHeight-34);
        var valid=samples.Where(s=>s.Value.HasValue && double.IsFinite(s.Value.Value)).ToArray();
        if(valid.Length==0){Label("Chưa có mẫu đo",10,h/2);return;}
        double max=Percent?100:Math.Max(1,valid.Max(s=>s.Value!.Value));
        var grid=new Pen((Brush)FindResource("BorderBrushSubtle"),0.6);
        var color=(Brush)FindResource("AccentBrush");
        var pen=new Pen(color,2);
        for(int i=0;i<=2;i++){
            double y=8+h*i/2;dc.DrawLine(grid,new Point(32,y),new Point(32+w,y));
            Label((max*(1-i/2.0)).ToString("0.#"),0,y-6);
        }
        var start=samples[0].At;
        double duration=Math.Max(1,(samples[^1].At-start).TotalSeconds);
        Point? previous=null;
        foreach(var sample in samples){
            if(sample.Value is not double value || !double.IsFinite(value)){previous=null;continue;}
            var pt=new Point(32+w*(sample.At-start).TotalSeconds/duration,8+h*(1-Math.Clamp(value/max,0,1)));
            if(previous is Point prev)dc.DrawLine(pen,prev,pt);
            dc.DrawEllipse(color,null,pt,2,2);previous=pt;
        }
        string format=duration>86400?"dd/MM":"HH:mm";
        Label(start.ToLocalTime().ToString(format),32,h+12);
        if(samples.Length>1)Label(samples[^1].At.ToLocalTime().ToString(format),w,h+12);
    }
}
