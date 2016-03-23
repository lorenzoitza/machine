﻿using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.HermitCrab.MorphologicalRules;
using SIL.ObjectModel;

namespace SIL.Machine.HermitCrab
{
	public class Word : Freezable<Word>, IAnnotatedData<ShapeNode>, ICloneable<Word>
	{
		private readonly Dictionary<string, Allomorph> _allomorphs; 
		private RootAllomorph _rootAllomorph;
		private Shape _shape;
		private readonly Stack<IMorphologicalRule> _mrules;
		private readonly Dictionary<IMorphologicalRule, int> _mrulesUnapplied;
		private readonly Dictionary<IMorphologicalRule, int> _mrulesApplied;
		private readonly Stack<Word> _nonHeads;
		private readonly MprFeatureSet _mprFeatures;
		private readonly IDBearerSet<Feature> _obligatorySyntacticFeatures;
		private FeatureStruct _realizationalFS;
		private Stratum _stratum;
		private bool _isLastAppliedRuleFinal;

		public Word(RootAllomorph rootAllomorph, FeatureStruct realizationalFS)
		{
			_allomorphs = new Dictionary<string, Allomorph>();
			_mprFeatures = new MprFeatureSet();
			_shape = rootAllomorph.Shape.Clone();
			ResetDirty();
			SetRootAllomorph(rootAllomorph);
			RealizationalFeatureStruct = realizationalFS;
			_mrules = new Stack<IMorphologicalRule>();
			_mrulesUnapplied = new Dictionary<IMorphologicalRule, int>();
			_mrulesApplied = new Dictionary<IMorphologicalRule, int>();
			_nonHeads = new Stack<Word>();
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>();
			_isLastAppliedRuleFinal = true;
		}

		public Word(Stratum stratum, Shape shape)
		{
			_allomorphs = new Dictionary<string, Allomorph>();
			Stratum = stratum;
			_shape = shape;
			ResetDirty();
			SyntacticFeatureStruct = new FeatureStruct();
			RealizationalFeatureStruct = new FeatureStruct();
			_mprFeatures = new MprFeatureSet();
			_mrules = new Stack<IMorphologicalRule>();
			_mrulesUnapplied = new Dictionary<IMorphologicalRule, int>();
			_mrulesApplied = new Dictionary<IMorphologicalRule, int>();
			_nonHeads = new Stack<Word>();
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>();
			_isLastAppliedRuleFinal = true;
		}

		protected Word(Word word)
		{
			_allomorphs = new Dictionary<string, Allomorph>(word._allomorphs);
			Stratum = word.Stratum;
			_shape = word._shape.Clone();
			_rootAllomorph = word._rootAllomorph;
			SyntacticFeatureStruct = word.SyntacticFeatureStruct.Clone();
			RealizationalFeatureStruct = word.RealizationalFeatureStruct.Clone();
			_mprFeatures = word.MprFeatures.Clone();
			_mrules = new Stack<IMorphologicalRule>(word._mrules.Reverse());
			_mrulesUnapplied = new Dictionary<IMorphologicalRule, int>(word._mrulesUnapplied);
			_mrulesApplied = new Dictionary<IMorphologicalRule, int>(word._mrulesApplied);
			_nonHeads = new Stack<Word>(word._nonHeads.Reverse().CloneItems());
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>(word._obligatorySyntacticFeatures);
			_isLastAppliedRuleFinal = word._isLastAppliedRuleFinal;
			CurrentTrace = word.CurrentTrace;
		}

		public IEnumerable<Annotation<ShapeNode>> Morphs
		{
			get { return Annotations.Where(ann => ann.Type() == HCFeatureSystem.Morph); }
		}

		public IEnumerable<Allomorph> AllomorphsInMorphOrder
		{
			get { return Morphs.Select(morph => _allomorphs[(string) morph.FeatureStruct.GetValue(HCFeatureSystem.Allomorph)]); }
		}

		public ICollection<Allomorph> Allomorphs
		{
			get { return _allomorphs.Values; }
		}

		public RootAllomorph RootAllomorph
		{
			get { return _rootAllomorph; }

			internal set
			{
				CheckFrozen();
				_shape = value.Shape.Clone();
				SetRootAllomorph(value);
			}
		}

		private void SetRootAllomorph(RootAllomorph rootAllomorph)
		{
			_rootAllomorph = rootAllomorph;
			var entry = (LexEntry) _rootAllomorph.Morpheme;
			Stratum = entry.Stratum;
			MarkMorph(_shape, _rootAllomorph);
			SyntacticFeatureStruct = entry.SyntacticFeatureStruct.Clone();
			_mprFeatures.Clear();
			_mprFeatures.UnionWith(entry.MprFeatures);
		}

		public Shape Shape
		{
			get { return _shape; }
		}

		public FeatureStruct SyntacticFeatureStruct { get; internal set; }

		public FeatureStruct RealizationalFeatureStruct
		{
			get { return _realizationalFS; }
			internal set
			{
				CheckFrozen();
				_realizationalFS = value;
			}
		}

		public MprFeatureSet MprFeatures
		{
			get { return _mprFeatures; }
		}

		public ICollection<Feature> ObligatorySyntacticFeatures
		{
			get { return _obligatorySyntacticFeatures; }
		}

		public Span<ShapeNode> Span
		{
			get { return _shape.Span; }
		}

		public AnnotationList<ShapeNode> Annotations
		{
			get { return _shape.Annotations; }
		}

		public Stratum Stratum
		{
			get { return _stratum; }
			internal set
			{
				CheckFrozen();
				_stratum = value;
			}
		}

		public object CurrentTrace { get; set; }

		public IEnumerable<IMorphologicalRule> MorphologicalRules
		{
			get { return _mrules; }
		}

		/// <summary>
		/// Gets the current rule.
		/// </summary>
		/// <value>The current rule.</value>
		internal IMorphologicalRule CurrentMorphologicalRule
		{
			get
			{
				if (_mrules.Count == 0)
					return null;
				return _mrules.Peek();
			}
		}

		internal Annotation<ShapeNode> MarkMorph(IEnumerable<ShapeNode> nodes, Allomorph allomorph)
		{
			ShapeNode[] nodeArray = nodes.ToArray();
			Annotation<ShapeNode> ann = null;
			if (nodeArray.Length > 0)
			{
				ann = new Annotation<ShapeNode>(_shape.SpanFactory.Create(nodeArray[0], nodeArray[nodeArray.Length - 1]), FeatureStruct.New()
					.Symbol(HCFeatureSystem.Morph)
					.Feature(HCFeatureSystem.Allomorph).EqualTo(allomorph.ID).Value);
				ann.Children.AddRange(nodeArray.Select(n => n.Annotation));
				_shape.Annotations.Add(ann, false);
			}
			_allomorphs[allomorph.ID] = allomorph;
			return ann;
		}

		internal void RemoveMorph(Annotation<ShapeNode> morphAnn)
		{
			var alloID = (string) morphAnn.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
			_allomorphs.Remove(alloID);
			foreach (ShapeNode node in _shape.GetNodes(morphAnn.Span).ToArray())
				node.Remove();
		}

		/// <summary>
		/// Notifies this analysis that the specified morphological rule was unapplied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		internal void MorphologicalRuleUnapplied(IMorphologicalRule mrule)
		{
			CheckFrozen();
			_mrulesUnapplied.UpdateValue(mrule, () => 0, count => count + 1);
			if (mrule is AffixProcessRule)
				_mrules.Push(mrule);
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been unapplied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of unapplications.</returns>
		internal int GetUnapplicationCount(IMorphologicalRule mrule)
		{
			int numUnapplies;
			if (!_mrulesUnapplied.TryGetValue(mrule, out numUnapplies))
				numUnapplies = 0;
			return numUnapplies;
		}

		/// <summary>
		/// Notifies this word synthesis that the specified morphological rule has applied.
		/// </summary>
		internal void MorphologicalRuleApplied(IMorphologicalRule mrule)
		{
			CheckFrozen();
			_mrulesApplied.UpdateValue(mrule, () => 0, count => count + 1);
		}

		internal void CurrentMorphologicalRuleApplied()
		{
			CheckFrozen();
			IMorphologicalRule mrule = _mrules.Pop();
			MorphologicalRuleApplied(mrule);
		}

		internal bool IsLastAppliedRuleFinal
		{
			get { return _isLastAppliedRuleFinal; }
			set
			{
				CheckFrozen();
				_isLastAppliedRuleFinal = value;
			}
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been applied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of applications.</returns>
		internal int GetApplicationCount(IMorphologicalRule mrule)
		{
			int numApplies;
			if (!_mrulesApplied.TryGetValue(mrule, out numApplies))
				numApplies = 0;
			return numApplies;
		}

		public Allomorph GetAllomorph(Annotation<ShapeNode> morph)
		{
			var alloID = (string) morph.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
			return _allomorphs[alloID];
		}

		internal Word CurrentNonHead
		{
			get
			{
				if (_nonHeads.Count == 0)
					return null;
				return _nonHeads.Peek();
			}
		}

		internal int NonHeadCount
		{
			get { return _nonHeads.Count; }
		}

		internal void NonHeadUnapplied(Word nonHead)
		{
			CheckFrozen();
			_nonHeads.Push(nonHead);
		}

		internal void CurrentNonHeadApplied()
		{
			CheckFrozen();
			_nonHeads.Pop();
		}

		internal bool CheckBlocking(out Word word)
		{
			word = null;
			LexFamily family = ((LexEntry) RootAllomorph.Morpheme).Family;
			if (family == null)
				return false;

			foreach (LexEntry entry in family.Entries)
			{
				if (entry != RootAllomorph.Morpheme && entry.Stratum == Stratum && SyntacticFeatureStruct.Subsumes(entry.SyntacticFeatureStruct))
				{
					word = new Word(entry.PrimaryAllomorph, RealizationalFeatureStruct.Clone()) { CurrentTrace = CurrentTrace };
					word.Freeze();
					return true;
				}
			}

			return false;
		}

		internal void ResetDirty()
		{
			CheckFrozen();
			foreach (ShapeNode node in _shape)
				node.SetDirty(false);
		}

		internal IDictionary<int, Tuple<FailureReason, object>> CurrentRuleResults { get; set; }

		protected override int FreezeImpl()
		{
			int code = 23;
			_shape.Freeze();
			code = code * 31 + _shape.GetFrozenHashCode();
			_realizationalFS.Freeze();
			code = code * 31 + _realizationalFS.GetFrozenHashCode();
			foreach (Word nonHead in _nonHeads)
			{
				nonHead.Freeze();
				code = code * 31 + nonHead.GetFrozenHashCode();
			}
			code = code * 31 + _stratum.GetHashCode();
			code = code * 31 + (_rootAllomorph == null ? 0 : _rootAllomorph.GetHashCode());
			code = code * 31 + _mrules.GetSequenceHashCode();
			code = code * 31 + _isLastAppliedRuleFinal.GetHashCode();
			return code;
		}

		public override bool ValueEquals(Word other)
		{
			if (other == null)
				return false;

			if (IsFrozen && other.IsFrozen && GetFrozenHashCode() != other.GetFrozenHashCode())
				return false;

			return _shape.ValueEquals(other._shape) && _realizationalFS.ValueEquals(other._realizationalFS)
				&& _nonHeads.SequenceEqual(other._nonHeads, FreezableEqualityComparer<Word>.Default) && _stratum == other._stratum
				&& _rootAllomorph == other._rootAllomorph && _mrules.SequenceEqual(other._mrules) && _isLastAppliedRuleFinal == other._isLastAppliedRuleFinal;
		}

		public Word Clone()
		{
			return new Word(this);
		}

		public override string ToString()
		{
			return Shape.ToRegexString(Stratum.SymbolTable, true);
		}
	}
}
