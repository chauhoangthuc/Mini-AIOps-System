using System.Windows;
using System.Windows.Media;
namespace ChatOps.Controls;

public sealed class StatusRing : FrameworkElement
{
    public static readonly DependencyProperty OpenProperty=DependencyProperty.Register(nameof(Open),typeof(int),typeof(StatusRing),new FrameworkPropertyMetadata(0,FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty PendingProperty=DependencyProperty.Register(nameof(Pending),typeof(int),typeof(StatusRing),new FrameworkPropertyMetadata(0,FrameworkPropertyMetadataOptions.AffectsRender));
    public int Open { get=>(int)GetValue(OpenProperty);set=>SetValue(OpenProperty,value); }
    public int Pending { get=>(int)GetValue(PendingProperty);set=>SetValue(PendingProperty,value); }
    protected override void OnRender(DrawingContext dc)
    {
        var center=new Point(ActualWidth/2,ActualHeight/2);
        var radius=Math.Max(1,Math.Min(ActualWidth,ActualHeight)/2-10);
        var fraction=Open>0?Math.Clamp((double)Pending/Open,0,1):0;
        Pen PenFor(string key)=>new((Brush)FindResource(key),12);
        dc.DrawEllipse(null,PenFor(Open==0?"BorderBrushSubtle":fraction==1?"WarningForeground":"DangerForeground"),center,radius,radius);
        if(fraction<=0 || fraction>=1)return;
        double angle=2*Math.PI*fraction;
        var geometry=new StreamGeometry();
        using(var ctx=geometry.Open()){
            ctx.BeginFigure(new Point(center.X,center.Y-radius),false,false);
            ctx.ArcTo(new Point(center.X+radius*Math.Sin(angle),center.Y-radius*Math.Cos(angle)),new Size(radius,radius),0,
                fraction>.5,SweepDirection.Clockwise,true,false);
        }
        dc.DrawGeometry(null,PenFor("WarningForeground"),geometry);
    }
}
