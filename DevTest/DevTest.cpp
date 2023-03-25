#include <iostream>
#include <string>
#include <fstream>
#include <vector>
using namespace std;

int main() {
    std::ifstream input_file("input.txt");
    std::vector<std::string> values;

    if (input_file)
    {
        char c;
        std::string current_value;

        // Read the input file character by character
        while (input_file.get(c))
        {
            // If the character is a comma, add the current value to the vector
            if (c == ',')
            {
                values.push_back(current_value);
                current_value.clear();
            }
            // If the character is a double quote, ignore it
            else if (c == '\"')
            {
                continue;
            }
            // Otherwise, add the character to the current value
            else
            {
                current_value += c;
            }
        }

        // Add the last value to the vector
        if (!current_value.empty())
        {
            values.push_back(current_value);
        }
    }

    // Write the extracted values to a file
    std::ofstream output_file("output.txt");
    if (output_file)
    {
        for (const auto& value : values)
        {
            output_file << value + "USDT," << std::endl;
        }
    }
    else
    {
        std::cerr << "Error: Could not open output file." << std::endl;
        return 1;
    }

    return 0;


    return 0;
}