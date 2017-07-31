﻿using Ikadn.Ikon.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Stareater.Utils.StateEngine
{
	class DictionaryStrategy : AEnumerableStrategy
	{
		public DictionaryStrategy(Type type)
			: base(type, BuildConstructor(type), CopyMethodInfo(type), SerializeMethodInfo(type))
		{ }
		
		private static void copyChildren<K, V>(IDictionary<K, V> originalDictionary, IDictionary<K, V> dictionaryCopy, CopySession session)
		{
			foreach (var element in originalDictionary)
				dictionaryCopy.Add((K)session.CopyOf(element.Key), (V)session.CopyOf(element.Value));
		}

		private static IkonBaseObject serializeChildren<K, V>(IDictionary<K, V> originalDictionary, SaveSession session)
		{
			var data = new IkonArray();

			foreach (var element in originalDictionary)
			{
				data.Add(session.Serialize(element.Key));
				data.Add(session.Serialize(element.Value));
			}

			return data;
		}

		private static Func<object, object> BuildConstructor(Type type)
		{
			var ctorInfo = type.GetConstructor(new Type[0]);
			var funcBody = Expression.New(ctorInfo);

			var expr =
				Expression.Lambda<Func<object, object>>(
					funcBody,
					Expression.Parameter(typeof(object), "o")
				);

			return expr.Compile();
		}

		private static MethodInfo CopyMethodInfo(Type type)
		{
            var interfaceType = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            return typeof(DictionaryStrategy).
				GetMethod("copyChildren", BindingFlags.NonPublic | BindingFlags.Static).
				MakeGenericMethod(type.GetGenericArguments());
		}

		private static MethodInfo SerializeMethodInfo(Type type)
		{
			var interfaceType = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));

			return typeof(DictionaryStrategy).
				GetMethod("serializeChildren", BindingFlags.NonPublic | BindingFlags.Static).
				MakeGenericMethod(type.GetGenericArguments());
		}
	}
}