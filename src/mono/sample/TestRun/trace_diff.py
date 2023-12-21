import sys
import re

def compare_stacks(file1_path, file2_path):
    try:
        with open(file1_path, "r") as file1, open(file2_path, "r") as file2:
            baseline_lines = file1.readlines()
            comparison_lines = file2.readlines()

        index1, index2 = 0, 0
        last_command = None

        while index1 < len(baseline_lines) and index2 < len(comparison_lines):
            stack1 = baseline_lines[index1].strip()
            stack2 = comparison_lines[index2].strip()

            # 使用正则表达式匹配重要信息
            pattern = re.compile(r'(\[([^:]+): ([\d.]+) (\d+)\]) (ENTER|LEAVE):c (.+?) (\(.*?\)|\()(\(.+?\)|\()')
            match1 = pattern.match(stack1)
            match2 = pattern.match(stack2)

            if match1 and match2:
                sameStackDeeps = match1.group(4) == match2.group(4)
                sameFuncAction = match1.group(5) == match2.group(5)
                sameFuncSig = match1.group(6) == match2.group(6) and match1.group(7) == match2.group(7)
                file1Param = match1.group(8)
                file2Param = match1.group(8)
                if sameStackDeeps and sameFuncAction and sameFuncSig:
                    # print(f"Stacks are identical at line {index1 + 1}")
                    index1 += 1
                    index2 += 1
                else:
                    print(f"Difference found at")
                    print(f"File 1, Line {index1 + 1}: {stack1}")
                    print(f"File 2, Line {index2 + 1}: {stack2}")
                    command = input("Enter '1' to move File 1 to the next line, '2' to move File 2 to the next line, 'q' to quit: ")

                    if command == '':
                        command = last_command

                    if command == '1':
                        index1 += 1
                    elif command == '2':
                        index2 += 1
                    elif command == 'q':
                        break
                    else:
                        print("Invalid command. Please enter '1', '2', or 'q'.")

                    last_command = command
            else:
                # 如果无法匹配到重要信息，直接跳到下一行
                print(stack1)
                print(stack2)
                index1 += 1
                index2 += 1

        print("Comparison complete.")

    except FileNotFoundError as e:
        print(f"Error: {e.filename} not found.")
    except Exception as e:
        print(f"An error occurred: {e}")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python trace_diff.py <baseline_file_path> <comparison_file_path>")
    else:
        baseline_file_path = sys.argv[1]
        comparison_file_path = sys.argv[2]
        compare_stacks(baseline_file_path, comparison_file_path)
