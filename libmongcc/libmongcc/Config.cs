using libcore;

namespace libmongcc
{
	class Config
	{
		public static TConfig getConfig()
		{
			TConfig config = new TConfig();

			config.enable	= StringUtils.convertString2Bool(SystemUtils.getEnvVarValue("MONGCC_ENABLE"));
			config.maxJobs	= StringUtils.convertString2Int(SystemUtils.getEnvVarValue("MONGCC_MAX_JOBS"));
			config.hosts	= StringUtils.splitBySeparate(SystemUtils.getEnvVarValue("MONGCC_HOSTS"));
			config.port		= StringUtils.convertString2Int(SystemUtils.getEnvVarValue("MONGCC_PORT"));
			config.timeout	= StringUtils.convertString2Int(SystemUtils.getEnvVarValue("MONGCC_TIMEOUT"));
			config.retryConnectTime = StringUtils.convertString2Int(SystemUtils.getEnvVarValue("MONGCC_RETRY_CONNECT_TIME"));

			if (config.enable && config.hosts != null)
			{
				if (config.port <= 0) config.port = s_kMongccPort;
				if (config.timeout <= 0) config.timeout = 180000;
				if (config.retryConnectTime <= 0) config.retryConnectTime = 30000;
			}

			if (config.maxJobs <= 0) config.maxJobs = 4;

			return config;
		}

		public static readonly int s_kMongccPort = 1909;
		public static readonly int s_kMongccBacklog = 4;
		public static readonly int s_kMaxAgentHandlers = 8;
		public static readonly string s_kAgentName = "MONGCC_AGENT";
	}
}
