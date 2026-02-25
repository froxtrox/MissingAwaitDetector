using MissingAwaitDetector.Analyzers.Sample;

Console.WriteLine("MissingAwaitDetector Sample — see each MAWT00x file for annotated examples.");

await MAWT001_TaskTreatedAsValue.GoodExamplesAsync();
await MAWT002_SynchronousUnwrap.GoodExamplesAsync();
await MAWT003_TaskStateInspection.GoodExamplesAsync();
await MAWT004_FireAndForget.GoodExamplesAsync();
await MAWT005_StoredNeverAwaited.GoodExamplesAsync();
await MAWT006_ValueTaskMisuse.GoodExamplesAsync();
await MAWT007_LinqTaskCollection.GoodExamplesAsync();
await MAWT008_SyncUnwrapInAsyncMethod.GoodExamplesAsync();
await MAWT009_TaskUsedBeforeReturn.GoodExamplesAsync();
await MAWT010_AsyncVoidMethod.GoodExamplesAsync();

Console.WriteLine("Done.");
