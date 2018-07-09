namespace FileHost.Infra
{
	public class FileSizeFormatter
	{
		private readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

		public string GetReadableFileSize(long sizeInBytes)
		{
			var unitIndex = 0;

			while (sizeInBytes >= 1024)
			{
				sizeInBytes /= 1024;
				++unitIndex;
			}

			var unit = Units[unitIndex];
			return string.Format("{0:0.#} {1}", sizeInBytes, unit);
		}
	}
}
