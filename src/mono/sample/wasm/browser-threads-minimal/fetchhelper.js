
export function delay(timeoutMs) {
  return new Promise(resolve => setTimeout(resolve, timeoutMs));
}

export async function responseText(response, timeoutMs) /* Promise<string> */ {
  console.log(`artificially waiting for response for ${timeoutMs} ms`);
  await delay(timeoutMs);
  console.log("artificial waiting done");
  return await response.text();
}
