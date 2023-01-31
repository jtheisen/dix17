namespace Dix17;

//public class Reconciler
//{
//    public Dix Copy(ISource source, ISource target)
//    {
//        var sourceDix = source.Query(Dq);

//        foreach (var item in sourceDix.GetStructure())
//        {
//            target.Query(+item);
//        }
//    }

//    public Dix Check(Stack<String?> path, ISource source, ISource target, Dix query, Dix response)
//    {
//        if (query.HasNilContent)
//        {
//            return response;
//        }
//        else if (response.HasNilContent)
//        {
//            // this an error?

//            var rerootPath = path.ToArray();

//            return Copy(source.Reroot(rerootPath!), target.Reroot(rerootPath!));
//        }
//        else
//        {
//            var queryChildren = query.GetStructure().ToArray();
//            var responseChildren = response.GetStructure().ToArray();

//            for (var i = 0; i < queryChildren.Length; i++)
//            {
//                var queryChild = queryChildren[i];

//                if (responseChildren.Length < i - 1) throw new Exception();

//                var responseChild = responseChildren[i];

//                if (queryChild.Name != responseChild.Name) throw new Exception();

//                path.Push(queryChild.Name);

//                try
//                {
//                    Check(path, source, target, queryChild, responseChild);
//                }
//                finally
//                {
//                    path.Pop();
//                }
//            }
//        }
//    }
//}
