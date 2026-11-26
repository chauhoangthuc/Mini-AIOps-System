using System.Windows;
using System.Windows.Controls;

namespace ChatOps.Controls;
public partial class StatusBadge : UserControl
{
    public static readonly DependencyProperty LabelProperty=DependencyProperty.Register(nameof(Label),typeof(string),typeof(StatusBadge),
        new PropertyMetadata("", (element,_)=>((StatusBadge)element).ApplyPalette()));
    public string? Label {get=>(string?)GetValue(LabelProperty);set=>SetValue(LabelProperty,value);}
    public StatusBadge()=>InitializeComponent();
    private void ApplyPalette()
    {
        var key=Label switch {
            "Cao" or "Lỗi" or "Thất bại"=>"DangerBadge",
            "Chờ phê duyệt" or "Đang xác minh" or "Trung bình"=>"WarningBadge",
            "Online" or "Đã phục hồi"=>"SuccessBadge",
            _=>"NeutralBadge"
        };
        if(Surface!=null)Surface.SetResourceReference(StyleProperty,key);
    }
}
