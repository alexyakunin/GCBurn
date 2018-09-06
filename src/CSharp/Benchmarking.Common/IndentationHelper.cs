using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;

namespace Benchmarking.Common 
{
    public static class IndentationHelper
    {
        public static int ValueColumnIndex = 20;
        public static int IndentSize = 2;

        public static string CreateIndentString() => string.Concat(Enumerable.Repeat(" ", IndentSize));
        
        public static IndentedTextWriter CreateWriter(TextWriter inner) => 
            new IndentedTextWriter(inner, CreateIndentString());
        
        public static IndentedTextWriter AddIndent(this IndentedTextWriter writer, int increment = 1)
        {
            writer.Indent += increment;
            return writer;
        }

        public static IndentedTextWriter RemoveIndent(this IndentedTextWriter writer, int decrement = 1)
        {
            writer.Indent -= decrement;
            return writer;
        }
        
        public static IDisposable Indent(this IndentedTextWriter writer, int indent = 1)
        {
            writer.AddIndent(indent);
            return Disposable.Create(writer, w => w.RemoveIndent(indent));
        }
        
        public static IDisposable Section(this IndentedTextWriter writer, string title, int indent = 1)
        {
            writer.WriteLine(title);
            return writer.Indent(indent);
        }
        
        public static IndentedTextWriter WithIndent(this IndentedTextWriter writer, Action action, int indent = 1)
        {
            if (indent == 0)
                action.Invoke();
            else
                using (writer.Indent(indent))
                    action.Invoke();
            return writer;
        }
        
        public static IndentedTextWriter Append(this IndentedTextWriter writer, string line)
        {
            writer.Write(line);
            return writer;
        }
        
        public static IndentedTextWriter AppendLine(this IndentedTextWriter writer, string line = "")
        {
            writer.WriteLine(line);
            return writer;
        }
        
        public static IndentedTextWriter AppendValue(this IndentedTextWriter writer, string name, string value)
        {
            var formattedName = string.Format($"{{0,-{Math.Max(0, ValueColumnIndex - writer.Indent * IndentSize)}}}", name);
            return writer.AppendLine($"{formattedName}: {value}");
        }

        public static IndentedTextWriter AppendMetric(this IndentedTextWriter writer, 
            string name, double? value, string unit, string format = "0.###") => 
            writer.AppendValue(name, value.ToString(format, unit));
    }
}
