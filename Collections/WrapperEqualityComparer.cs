﻿using System.Collections;
using System.Collections.Generic;

namespace SIL.Collections
{
	public class WrapperEqualityComparer<T> : EqualityComparer<T>
	{
		private readonly IEqualityComparer _comparer;

		public WrapperEqualityComparer(IEqualityComparer comparer)
		{
			_comparer = comparer;
		}

		public override bool Equals(T x, T y)
		{
			return _comparer.Equals(x, y);
		}

		public override int GetHashCode(T obj)
		{
			return _comparer.GetHashCode(obj);
		}
	}
}
