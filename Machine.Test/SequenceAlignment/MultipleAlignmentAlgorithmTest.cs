using System;
using System.Linq;
using NUnit.Framework;
using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Test.SequenceAlignment
{
	[TestFixture]
	public class MultipleAlignmentAlgorithmTest : AlignmentAlgorithmTestBase
	{
		[Test]
		public void AlignMoreThanTwoSequences()
		{
			var scorer = new StringScorer();
			var msa = new MultipleAlignmentAlgorithm<string, char>(scorer, new[] {"car", "bar", "carp"}, GetChars) {UseInputOrder = true};
			msa.Compute();
			Alignment<string, char> alignment = msa.GetAlignment();

			AssertAlignmentsEqual(alignment, CreateAlignment(
				"| c a r - |",
				"| b a r - |",
				"| c a r p |"
				));

			msa = new MultipleAlignmentAlgorithm<string, char>(scorer, new[] {"car", "bar", "star"}, GetChars) {UseInputOrder = true};
			msa.Compute();
			alignment = msa.GetAlignment();

			AssertAlignmentsEqual(alignment, CreateAlignment(
				"| - c a r |",
				"| - b a r |",
				"| s t a r |"
				));

			msa = new MultipleAlignmentAlgorithm<string, char>(scorer, new[] {"car", "bar", "stare"}, GetChars) {UseInputOrder = true};
			msa.Compute();
			alignment = msa.GetAlignment();

			AssertAlignmentsEqual(alignment, CreateAlignment(
				"| - c a r - |",
				"| - b a r - |",
				"| s t a r e |"
				));

			msa = new MultipleAlignmentAlgorithm<string, char>(scorer, new[] {"scar", "car", "bar", "stare"}, GetChars) {UseInputOrder = true};
			msa.Compute();
			alignment = msa.GetAlignment();

			AssertAlignmentsEqual(alignment, CreateAlignment(
				"| s c a r - |",
				"| - c a r - |",
				"| - b a r - |",
				"| s t a r e |"
				));

			msa = new MultipleAlignmentAlgorithm<string, char>(scorer, new[] {"sane", "scar", "car", "bar", "stare"}, GetChars) {UseInputOrder = true};
			msa.Compute();
			alignment = msa.GetAlignment();

			AssertAlignmentsEqual(alignment, CreateAlignment(
				"| s - a n e |",
				"| s c a r - |",
				"| - c a r - |",
				"| - b a r - |",
				"| s t a r e |"
				));

			msa = new MultipleAlignmentAlgorithm<string, char>(scorer, new[] {"sane", "scar", "she", "car", "bar", "stare"}, GetChars) {UseInputOrder = true};
			msa.Compute();
			alignment = msa.GetAlignment();

			AssertAlignmentsEqual(alignment, CreateAlignment(
				"| s - a n e |",
				"| s c a r - |",
				"| s - - h e |",
				"| - c a r - |",
				"| - b a r - |",
				"| s t a r e |"
				));
		}

		[Test]
		public void AlignLessThanThreeSequences()
		{
			var scorer = new StringScorer();
			Assert.Throws<ArgumentException>(() =>
				{
					var msa = new MultipleAlignmentAlgorithm<string, char>(scorer, Enumerable.Empty<string>(), GetChars);
				});

			Assert.Throws<ArgumentException>(() =>
				{
					var msa = new MultipleAlignmentAlgorithm<string, char>(scorer, new[] {"bar"}, GetChars);
				});

				Assert.Throws<ArgumentException>(() =>
				{
					var msa = new MultipleAlignmentAlgorithm<string, char>(scorer, new[] {"car", "bar"}, GetChars);
				});
		}

		[Test]
		public void AlignEmptySequence()
		{
			var scorer = new StringScorer();
			var msa = new MultipleAlignmentAlgorithm<string, char>(scorer, new[] {"car", "", "bar"}, GetChars) {UseInputOrder = true};
			msa.Compute();
			Alignment<string, char> alignment = msa.GetAlignment();

			AssertAlignmentsEqual(alignment, CreateAlignment(
				"| c a r |",
				"| - - - |",
				"| b a r |"
				));
		}
	}
}
