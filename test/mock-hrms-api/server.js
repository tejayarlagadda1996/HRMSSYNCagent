const express = require("express");

const PORT = process.env.PORT || 3000;
const API_KEY = process.env.API_KEY || "test-api-key-123";

const app = express();
app.use(express.json({ limit: "2mb" }));

const receivedBatches = [];

function authorize(req, res, next) {
  const header = req.headers.authorization || "";
  const token = header.startsWith("Bearer ") ? header.slice(7) : "";

  if (token !== API_KEY) {
    return res.status(401).json({
      success: false,
      message: "Invalid API key",
      acceptedCount: 0,
    });
  }

  next();
}

app.get("/health", (_req, res) => {
  res.json({ status: "ok", batchesReceived: receivedBatches.length });
});

app.post("/api/attendance/sync", authorize, (req, res) => {
  const { companyCode, logs = [] } = req.body || {};

  if (!companyCode) {
    return res.status(400).json({
      success: false,
      message: "companyCode is required",
      acceptedCount: 0,
    });
  }

  const batch = {
    receivedAt: new Date().toISOString(),
    companyCode,
    count: logs.length,
    logs,
  };

  receivedBatches.push(batch);

  console.log(
    `[${batch.receivedAt}] ${companyCode}: received ${logs.length} log(s)`
  );

  if (logs.length > 0) {
    const sample = logs[0];
    console.log(
      `  first log -> employee=${sample.employeeCode}, deviceLogId=${sample.deviceLogId}, direction=${sample.direction}`
    );
  }

  res.json({
    success: true,
    message: `Accepted ${logs.length} record(s)`,
    acceptedCount: logs.length,
  });
});

app.get("/api/attendance/received", authorize, (_req, res) => {
  res.json({
    success: true,
    batches: receivedBatches,
    totalLogs: receivedBatches.reduce((sum, b) => sum + b.count, 0),
  });
});

app.listen(PORT, () => {
  console.log(`Mock HRMS API listening on http://localhost:${PORT}`);
  console.log(`API key: ${API_KEY}`);
  console.log(`Sync endpoint: POST http://localhost:${PORT}/api/attendance/sync`);
});
