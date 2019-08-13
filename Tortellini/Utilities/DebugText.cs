using Godot;
using System.Collections.Generic;
using System.Linq;

public class DebugText : RichTextLabel
{
    private static DebugText instance;

    private Dictionary<string, string> lines;
    public override void _Ready()
    {
        lines = new Dictionary<string, string>();
        DebugText.instance = this;
    }

    public override void _Process(float delta) {
        List<string> textLines;
        textLines = lines.Values.ToList();
        Clear();

        foreach (string line in textLines)
        {
            AppendBbcode(line);
            Newline();
        }
    }

    public static void Display(string id, string info) {
        if(instance == null) return;

        if(instance.lines.ContainsKey(id)) {
            instance.lines[id] = info;
        } else {
            instance.lines.Add(id, info);
        }
    }

    public static void Remove(string id) {
        if(instance == null) return;

        if(instance.lines.ContainsKey(id)) instance.lines.Remove(id);
    }

    public static void Reset() {
        if(instance != null) return;

        instance.lines.Clear();
    }
}
