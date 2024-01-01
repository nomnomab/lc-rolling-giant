using LethalSettings.UI.Components;
using UnityEngine;

namespace RollingGiant.Settings.Elements; 

public class HeaderFieldComponent: MenuComponent {
    public string Text { internal get; set; }
    public MenuComponent Child { internal get; set; }
    
    public override GameObject Construct(GameObject root) {
        var label = new LabelComponent {
            Text = Text,
        };
        
        var header = new VerticalComponent {
            Children = new[] {
                label,
                Child,
            }
        };
        
        return header.Construct(root);
    }
}