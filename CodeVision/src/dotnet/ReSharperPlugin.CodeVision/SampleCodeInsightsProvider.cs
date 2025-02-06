using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.Rider.Model;
using Clipboard = JetBrains.Application.UI.Components.Clipboard;
using MessageBox = JetBrains.Util.MessageBox;

namespace ReSharperPlugin.CodeVision;

// [SolutionComponent]
public class SampleCodeInsightsProvider : ICodeInsightsProvider
{
    public bool IsAvailableIn(ISolution solution)
    {
        return true;
    }

    public void OnClick(CodeInsightHighlightInfo highlightInfo, ISolution solution)
    {
        if (highlightInfo.CodeInsightsHighlighting.Entry is TextCodeVisionEntry)
        {
            TextCodeVisionEntry entry =
                (TextCodeVisionEntry)highlightInfo.CodeInsightsHighlighting.Entry;
            if (entry.LongPresentation != "")
            {
                // MessageBox.ShowInfo(entry.LongPresentation, "ReSharper SDK OnClick");
                System.Windows.Forms.Clipboard.SetText(entry.LongPresentation);
            }
            else
            {
                MessageBox.ShowInfo("广告位招租", "点这个也没用的啦");
            }
        }
    }

    public void OnExtraActionClick(CodeInsightHighlightInfo highlightInfo, string actionId, ISolution solution)
    {
        if (highlightInfo.CodeInsightsHighlighting.Entry is TextCodeVisionEntry)
        {
            TextCodeVisionEntry entry =
                (TextCodeVisionEntry)highlightInfo.CodeInsightsHighlighting.Entry;
            if (entry.LongPresentation != "")
            {
                // MessageBox.ShowInfo(entry.LongPresentation, "ReSharper SDK OnExtraActionClick");
                System.Windows.Forms.Clipboard.SetText(entry.LongPresentation);
            }
            else
            {
                MessageBox.ShowInfo("广告位招租", "点这个也没用的啦");
            }
        }
    }

    public string ProviderId => nameof(SampleCodeInsightsProvider);
    // public string DisplayName => $"ReSharper SDK: {nameof(SampleCodeInsightsProvider)}.{nameof(DisplayName)}";
    public string DisplayName => $"base64 txt";

    public CodeVisionAnchorKind DefaultAnchor => CodeVisionAnchorKind.Top;

    public ICollection<CodeVisionRelativeOrdering> RelativeOrderings => new List<CodeVisionRelativeOrdering>
        { new CodeVisionRelativeOrderingFirst() };
}
