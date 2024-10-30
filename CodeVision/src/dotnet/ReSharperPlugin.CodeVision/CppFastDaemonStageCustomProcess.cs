
using System;
using System.Text.RegularExpressions;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.VCXProj;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Cpp.Daemon.Highlightings;
using JetBrains.ReSharper.Feature.Services.Cpp.Daemon.InlayHints;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Features.Internal.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Interfaces;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Backend.Platform.Icons;
using JetBrains.Util;
using ReSharperPlugin.CodeVision;

#nullable disable
namespace JetBrains.ReSharper.Feature.Services.Cpp.Daemon
{
  public class CppFastDaemonStageCustomProcess : CppDaemonStageProcess
  {
    public static readonly Key<INamingPolicyProvider> NamingPolicyProviderKey = new Key<INamingPolicyProvider>("NamingPolicyProvider");
    private readonly bool myIsUESolution;
    private readonly SampleCodeInsightsProvider _codeInsightsProvider = new SampleCodeInsightsProvider();

    public CppFastDaemonStageCustomProcess(
      IDaemonProcess process,
      ElementProblemAnalyzerRegistrar registrar,
      DaemonProcessKind kind,
      IContextBoundSettingsStore settings,
      CppFile file,
      bool isUESolution,
      bool isRadler)
      : base(process, registrar, kind, settings, file, isRadler)
    {
      this.myIsUESolution = isUESolution;
    }

    public override void VisitNode(ITreeNode element, IHighlightingConsumer consumer)
    {
      base.VisitNode(element, consumer);
      CppFastDaemonStageCustomProcess.HighlightInvalidLineContinuation(element, consumer);

#if RIDER
      if(!element.IsCommentToken())
        return;
      string text = element.GetText();
      text += "\n";
      string spliter = "@b64:"; 
      if (!text.Contains(spliter))
        return;

      string pattern = @"@b64:[A-Za-z0-9+/=]+\n";
        
      MatchCollection matches = Regex.Matches(text, pattern);
      if (matches.Count == 0)
      {
        pattern = @"@b64:[^\n]+\n";
        matches = Regex.Matches(text, pattern);
        if (matches.Count == 1)
        {
          // add button to copy encoded text to clipboard
          string temp = matches[0].Value.Replace("@b64:", "").Replace("\n", "");
          byte[] data = System.Text.Encoding.UTF8.GetBytes(temp);
          string encodedString = Convert.ToBase64String(data);
          
          consumer.AddHighlighting(
            new CodeInsightsHighlighting(
              element.GetNavigationRange(),
              // element.GetNameDocumentRange(),
              // DocumentRange.InvalidRange,
              displayText: "copy encode base64",
              tooltipText: "copy encode base64",
              moreText: encodedString,
              _codeInsightsProvider,
              (IDeclaredElement) null,
              null));
        }
      }
      else
      {
        string decodedString = "";
        foreach (Match match in matches)
        {
          string temp = match.Value.Replace("@b64:", "").Replace("\n", "");
          byte[] data = Convert.FromBase64String(temp);
          if (decodedString == "")
          {
            decodedString += System.Text.Encoding.UTF8.GetString(data);
          }
          else
          {
            decodedString += " ";
            decodedString += System.Text.Encoding.UTF8.GetString(data);
          }
        }
        consumer.AddHighlighting(
          new CodeInsightsHighlighting(
            element.GetNavigationRange(),
            // element.GetNameDocumentRange(),
            // DocumentRange.InvalidRange,
            displayText: decodedString,//"ReSharper SDK: displayText",
            tooltipText: decodedString,//"ReSharper SDK: tooltipText",
            moreText: "",//"ReSharper SDK: moreText",
            _codeInsightsProvider,
            (IDeclaredElement) null,
            null));
      }
#endif
    }

    public static void HighlightInvalidLineContinuation(
      ITreeNode element,
      IHighlightingConsumer consumer)
    {
      DocumentRange DocRange;
      if (element.NodeType == CppTokenNodeTypes.EOL_COMMENT)
      {
        int lineCommentLength = GetTrimmedLineCommentLength(element.GetText());
        if (lineCommentLength == -1)
          return;
        DocRange = CppHighlightingBase.GetDocumentRangeOfNode(element).TrimLeft(lineCommentLength - 1);
      }
      else
      {
        if (element.NodeType != CppTokenNodeTypes.EOL_ESCAPE)
          return;
        string text = element.GetText();
        if (StripSpacesFromEolEscape(text) == text)
          return;
        DocRange = CppHighlightingBase.GetDocumentRangeOfNode(element);
      }
      CppHighlightingBase highlightingBase = (CppHighlightingBase) new CppInvalidLineContinuationWarning(DocRange, (ITokenNode) element);
      consumer.AddHighlighting((IHighlighting) highlightingBase);

      static int GetTrimmedLineCommentLength(string str)
      {
        for (int index = str.Length - 1; index >= 0; --index)
        {
          if (index != str.Length - 1 && str[index] == '\\')
            return index + 1;
          if (str[index] != ' ' && str[index] != '\t')
            return -1;
        }
        return -1;
      }

      static string StripSpacesFromEolEscape(string str)
      {
        return !str.EndsWith("\r\n") ? "\\\n" : "\\\r\n";
      }
    }

    protected override void FillData(ElementProblemAnalyzerData data)
    {
      NamingPolicyManager component = this.DaemonProcess.Solution.GetComponent<NamingPolicyManager>();
      data.PutData<INamingPolicyProvider>(CppFastDaemonStageCustomProcess.NamingPolicyProviderKey, component.GetPolicyProvider((PsiLanguageType) CppLanguage.Instance, this.DaemonProcess.SourceFile, this.mySettingsStore));
    }

    protected override bool AcceptAnalyzer(IElementProblemAnalyzer analyzer)
    {
      bool flag;
      switch (analyzer)
      {
        case ICppSlowElementProblemAnalyzer _:
        case ICppInlayHintsAnalyzer _:
          flag = true;
          break;
        default:
          flag = false;
          break;
      }
      return !flag && (this.myIsUESolution || !(analyzer is ICppUEElementProblemAnalyzer)) && (this.IsRadler || !(analyzer is CppMainRunnerLocalAnalyzer));
    }

    protected override string StageName => "FastStage";

    protected override void HighlightWholeFileErrors(IHighlightingConsumer consumer)
    {
      CppFastDaemonStageCustomProcess.HighlightWholeFileErrors(this.File, this.Data, consumer);
    }

    private static void HighlightPrecompiledHeaderErrors(
      CppFile file,
      IHighlightingConsumer consumer)
    {
      if (file.InclusionContext.RootContext.CompilationProperties.UsePrecompiledHeader == PchOption.None || file.InclusionContext.RootContext.PchInclusionStatus == CppPchInclusionStatus.IncludedByForceInclude || !file.InclusionContext.BaseFile.Equals(file.InclusionContext.GetRootBaseFile()) || string.IsNullOrWhiteSpace(file.InclusionContext.RootContext.CompilationProperties.PrecompiledHeaderThrough))
        return;
      if (file.InclusionContext.RootContext.CompilationProperties.UsePrecompiledHeader == PchOption.CreateUsingSpecific)
      {
        CppFileLocation pchFileLocation = CppPchCache.GetPchFileLocation(file.InclusionContext);
        foreach (CppFileSymbolTable processedSymbolTable in file.InclusionContext.RootContext.ProcessedSymbolTables)
        {
          if (processedSymbolTable.File.Equals(pchFileLocation))
            return;
        }
      }
      ITreeNode treeNode = (ITreeNode) null;
      foreach (ITreeNode filteredChild in file.GetFilteredChildren())
      {
        if (filteredChild is ImportDirective importDirective && importDirective.IsPchInclude)
          return;
        ITreeNode firstTokenIn = (ITreeNode) filteredChild.FindFirstTokenIn();
        if (firstTokenIn != null)
        {
          treeNode = firstTokenIn;
          break;
        }
      }
      if (treeNode == null)
        treeNode = file.FirstChild;
      if (treeNode == null)
        return;
      if (CppPchCache.GetPchFileLocation(file.InclusionContext).IsValid())
        consumer.AddHighlighting((IHighlighting) new CppPrecompiledHeaderIsNotIncludedError(treeNode));
      else
        consumer.AddHighlighting((IHighlighting) new CppPrecompiledHeaderNotFoundError(treeNode));
    }

    private static void HighlightMissingIncludeGuard(CppFile file, IHighlightingConsumer consumer)
    {
      if (!file.NeedsIncludeGuard())
        return;
      bool includeGuardFound;
      ITreeNode Anchor = CppTreeModificationUtil.SkipIncludeGuard(file, out includeGuardFound);
      if (includeGuardFound)
        return;
      while (Anchor != null && CppTokenNodeTypes.WHITESPACES[Anchor.NodeType])
        Anchor = Anchor.PrevSibling;
      ITreeNode node = Anchor ?? (ITreeNode) file.FindFirstTokenIn();
      ITreeNode ErrorNode = (node != null ? (ITreeNode) node.GetNextMeaningfulToken(true) : (ITreeNode) null) ?? node;
      consumer.AddHighlighting((IHighlighting) new CppMissingIncludeGuardWarning(ErrorNode, Anchor));
    }

    private static void HighlightWholeFileErrors(
      CppFile file,
      ElementProblemAnalyzerData data,
      IHighlightingConsumer consumer)
    {
      CppFastDaemonStageCustomProcess.HighlightPrecompiledHeaderErrors(file, consumer);
      CppFastDaemonStageCustomProcess.HighlightMissingIncludeGuard(file, consumer);
      CppUnmatchedDirectiveAnalyzer.Highlight(file, data, consumer);
      // CppUE4MiscAnalyzer.HighlightUE4GeneratedFileIsNotIncludedLastError(file, consumer);
      CppUE4MiscAnalyzer.HighlightUE4MissingStandardLibrary(file, consumer);
      CppUE4MiscAnalyzer.HighlightUE4IncorrectProjectFiles(file, consumer);
    }
  }
}
