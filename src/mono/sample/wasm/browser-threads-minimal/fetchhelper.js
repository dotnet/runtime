
export function delay(timeoutMs) {
  return new Promise(resolve => setTimeout(resolve, timeoutMs));
}

export async function responseText(response) /* Promise<string> */ {
  console.log("artificially waiting for response for 5 seconds");
  await delay(5000);
  console.log("artificial waiting done");
  return await response.text();
}
