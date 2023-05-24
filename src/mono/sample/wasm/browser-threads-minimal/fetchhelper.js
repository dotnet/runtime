
export function delay(timeoutMs) {
  return new Promise(resolve => setTimeout(resolve, timeoutMs));
}

export async function responseText(response, delay) /* Promise<string> */ {
  console.log(`artificially waiting for response for ${delay} ms`);
  await delay(delay);
  console.log("artificial waiting done");
  return await response.text();
}
